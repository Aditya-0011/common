package grpcx

import (
	"context"
	"errors"
	"fmt"
	"log/slog"
	"net"
	"os"
	"os/signal"
	"strconv"
	"syscall"
	"time"

	"buf.build/go/protovalidate"
	"google.golang.org/grpc"
	"google.golang.org/grpc/credentials"
	"google.golang.org/grpc/health"
	healthpb "google.golang.org/grpc/health/grpc_health_v1"
	"google.golang.org/grpc/keepalive"
)

const minTokenLength = 32

type ServerConfig struct {
	Addr                 string
	AuthToken            string
	Timeout              time.Duration
	TLSCertFile          string
	TLSKeyFile           string
	MaxConcurrentStreams uint32
}

func ServerConfigFromEnv(defaultPort string) (ServerConfig, error) {
	bindIP := os.Getenv("INTERNAL_BIND_IP")
	if bindIP == "" {
		return ServerConfig{}, errors.New("INTERNAL_BIND_IP is not set (use the private IP the gateway connects to)")
	}
	if net.ParseIP(bindIP) == nil {
		return ServerConfig{}, fmt.Errorf("INTERNAL_BIND_IP %q is not an IP address", bindIP)
	}

	port := os.Getenv("PORT")
	if port == "" {
		port = defaultPort
	}

	token, err := TokenFromEnv()
	if err != nil {
		return ServerConfig{}, err
	}

	timeout := 5 * time.Second
	if raw := os.Getenv("RPC_TIMEOUT_MS"); raw != "" {
		ms, err := strconv.Atoi(raw)
		if err != nil || ms <= 0 {
			return ServerConfig{}, fmt.Errorf("RPC_TIMEOUT_MS %q must be a positive integer", raw)
		}
		timeout = time.Duration(ms) * time.Millisecond
	}

	cfg := ServerConfig{
		Addr:                 net.JoinHostPort(bindIP, port),
		AuthToken:            token,
		Timeout:              timeout,
		TLSCertFile:          os.Getenv("INTERNAL_TLS_CERT"),
		TLSKeyFile:           os.Getenv("INTERNAL_TLS_KEY"),
		MaxConcurrentStreams: 100,
	}
	if (cfg.TLSCertFile == "") != (cfg.TLSKeyFile == "") {
		return ServerConfig{}, errors.New("INTERNAL_TLS_CERT and INTERNAL_TLS_KEY must be set together")
	}
	return cfg, nil
}

func TokenFromEnv() (string, error) {
	token := os.Getenv("INTERNAL_AUTH_TOKEN")
	if len(token) < minTokenLength {
		return "", fmt.Errorf("INTERNAL_AUTH_TOKEN must be set and at least %d characters", minTokenLength)
	}
	return token, nil
}

type Server struct {
	*grpc.Server
	health *health.Server
	addr   string
}

func NewServer(cfg ServerConfig, validator protovalidate.Validator) (*Server, error) {
	if len(cfg.AuthToken) < minTokenLength {
		return nil, fmt.Errorf("auth token must be at least %d characters", minTokenLength)
	}
	if cfg.Timeout <= 0 {
		return nil, errors.New("timeout must be positive")
	}

	opts := []grpc.ServerOption{
		grpc.ChainUnaryInterceptor(
			recoveryInterceptor(),
			loggingInterceptor(),
			authInterceptor(cfg.AuthToken),
			deadlineInterceptor(cfg.Timeout),
			validationInterceptor(validator),
		),
		grpc.MaxConcurrentStreams(cfg.MaxConcurrentStreams),
		grpc.KeepaliveParams(keepalive.ServerParameters{
			MaxConnectionIdle: 5 * time.Minute,
			MaxConnectionAge:  30 * time.Minute,
			Time:              1 * time.Minute,
			Timeout:           20 * time.Second,
		}),
		grpc.KeepaliveEnforcementPolicy(keepalive.EnforcementPolicy{
			MinTime:             30 * time.Second,
			PermitWithoutStream: false,
		}),
		grpc.MaxRecvMsgSize(4 * 1024 * 1024),
	}

	if cfg.TLSCertFile != "" {
		creds, err := credentials.NewServerTLSFromFile(cfg.TLSCertFile, cfg.TLSKeyFile)
		if err != nil {
			return nil, fmt.Errorf("loading TLS certificate: %w", err)
		}
		opts = append(opts, grpc.Creds(creds))
	}

	srv := grpc.NewServer(opts...)
	hs := health.NewServer()
	healthpb.RegisterHealthServer(srv, hs)

	return &Server{Server: srv, health: hs, addr: cfg.Addr}, nil
}

func (s *Server) Run(shutdownTimeout time.Duration) error {
	lis, err := net.Listen("tcp", s.addr)
	if err != nil {
		return fmt.Errorf("listen on %s: %w", s.addr, err)
	}

	ctx, stop := signal.NotifyContext(context.Background(), os.Interrupt, syscall.SIGTERM)
	defer stop()

	serveErr := make(chan error, 1)
	go func() {
		slog.Info("gRPC server listening", slog.String("address", lis.Addr().String()))
		serveErr <- s.Serve(lis)
	}()
	s.health.SetServingStatus("", healthpb.HealthCheckResponse_SERVING)

	var runErr error
	select {
	case <-ctx.Done():
		slog.Info("shutdown signal received")
	case runErr = <-serveErr:
		slog.Error("gRPC server stopped unexpectedly", slog.Any("error", runErr))
	}

	s.health.Shutdown()

	stopped := make(chan struct{})
	go func() {
		s.GracefulStop()
		close(stopped)
	}()

	select {
	case <-stopped:
		slog.Info("gRPC server stopped gracefully")
	case <-time.After(shutdownTimeout):
		slog.Warn("graceful shutdown timed out, forcing stop")
		s.Stop()
	}
	return runErr
}
