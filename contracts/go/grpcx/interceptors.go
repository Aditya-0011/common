package grpcx

import (
	"context"
	"crypto/sha256"
	"crypto/subtle"
	"log/slog"
	"runtime/debug"
	"strings"
	"time"

	"buf.build/go/protovalidate"
	"google.golang.org/grpc"
	"google.golang.org/grpc/codes"
	"google.golang.org/grpc/metadata"
	"google.golang.org/grpc/status"
	"google.golang.org/protobuf/proto"
)

const (
	errMissingServiceCredentials = "missing service credentials"
	errInvalidServiceCredentials = "invalid service credentials"

	authMetadataKey = "authorization"
	bearerPrefix    = "Bearer "
	healthPrefix    = "/grpc.health.v1.Health/"
)

func recoveryInterceptor() grpc.UnaryServerInterceptor {
	return func(ctx context.Context, req any, info *grpc.UnaryServerInfo, handler grpc.UnaryHandler) (resp any, err error) {
		defer func() {
			if r := recover(); r != nil {
				slog.LogAttrs(ctx, slog.LevelError, "panic in gRPC handler",
					slog.String("method", info.FullMethod),
					slog.Any("panic", r),
					slog.String("stack", string(debug.Stack())),
				)
				err = status.Error(codes.Internal, "Internal server error")
			}
		}()
		return handler(ctx, req)
	}
}

func loggingInterceptor() grpc.UnaryServerInterceptor {
	return func(ctx context.Context, req any, info *grpc.UnaryServerInfo, handler grpc.UnaryHandler) (any, error) {
		start := time.Now()
		resp, err := handler(ctx, req)

		st := status.Convert(err)
		attrs := []slog.Attr{
			slog.String("method", info.FullMethod),
			slog.String("status", st.Code().String()),
			slog.Duration("duration", time.Since(start)),
		}
		if err != nil {
			attrs = append(attrs, slog.String("error", st.Message()))
		}
		slog.LogAttrs(ctx, levelFor(st.Code()), "gRPC request", attrs...)
		return resp, err
	}
}

func levelFor(code codes.Code) slog.Level {
	switch code {
	case codes.OK, codes.Canceled, codes.InvalidArgument, codes.NotFound, codes.AlreadyExists, codes.Unauthenticated:
		return slog.LevelInfo
	case codes.DeadlineExceeded, codes.PermissionDenied, codes.ResourceExhausted, codes.FailedPrecondition,
		codes.Aborted, codes.OutOfRange, codes.Unavailable:
		return slog.LevelWarn
	default:
		return slog.LevelError
	}
}

func authInterceptor(token string) grpc.UnaryServerInterceptor {
	want := sha256.Sum256([]byte(token))

	return func(ctx context.Context, req any, info *grpc.UnaryServerInfo, handler grpc.UnaryHandler) (any, error) {
		if strings.HasPrefix(info.FullMethod, healthPrefix) {
			return handler(ctx, req)
		}

		md, _ := metadata.FromIncomingContext(ctx)
		values := md.Get(authMetadataKey)
		if len(values) != 1 || !strings.HasPrefix(values[0], bearerPrefix) {
			return nil, status.Error(codes.PermissionDenied, errMissingServiceCredentials)
		}

		got := sha256.Sum256([]byte(strings.TrimPrefix(values[0], bearerPrefix)))
		if subtle.ConstantTimeCompare(got[:], want[:]) != 1 {
			return nil, status.Error(codes.PermissionDenied, errInvalidServiceCredentials)
		}
		return handler(ctx, req)
	}
}

func deadlineInterceptor(d time.Duration) grpc.UnaryServerInterceptor {
	return func(ctx context.Context, req any, _ *grpc.UnaryServerInfo, handler grpc.UnaryHandler) (any, error) {
		ctx, cancel := context.WithTimeout(ctx, d)
		defer cancel()
		return handler(ctx, req)
	}
}

func validationInterceptor(validator protovalidate.Validator) grpc.UnaryServerInterceptor {
	return func(ctx context.Context, req any, _ *grpc.UnaryServerInfo, handler grpc.UnaryHandler) (any, error) {
		if msg, ok := req.(proto.Message); ok {
			if err := validator.Validate(msg); err != nil {
				return nil, status.Error(codes.InvalidArgument, err.Error())
			}
		}
		return handler(ctx, req)
	}
}

func IsServiceCredentialsError(err error) bool {
	st, ok := status.FromError(err)
	if !ok || st.Code() != codes.PermissionDenied {
		return false
	}
	return st.Message() == errMissingServiceCredentials || st.Message() == errInvalidServiceCredentials
}
