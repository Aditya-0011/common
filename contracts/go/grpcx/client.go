package grpcx

import (
	"context"
	"errors"
	"fmt"
	"time"

	"google.golang.org/grpc"
	"google.golang.org/grpc/credentials"
	"google.golang.org/grpc/credentials/insecure"
	"google.golang.org/grpc/keepalive"
)

type ClientConfig struct {
	Addr          string
	AuthToken     string
	TLSCAFile     string
	TLSServerName string
}

func Dial(cfg ClientConfig) (*grpc.ClientConn, error) {
	if cfg.Addr == "" {
		return nil, errors.New("service address is empty")
	}
	if len(cfg.AuthToken) < minTokenLength {
		return nil, fmt.Errorf("auth token must be at least %d characters", minTokenLength)
	}

	transport := insecure.NewCredentials()
	secure := false
	if cfg.TLSCAFile != "" {
		creds, err := credentials.NewClientTLSFromFile(cfg.TLSCAFile, cfg.TLSServerName)
		if err != nil {
			return nil, fmt.Errorf("loading TLS CA: %w", err)
		}
		transport = creds
		secure = true
	}

	return grpc.NewClient(
		cfg.Addr,
		grpc.WithTransportCredentials(transport),
		grpc.WithPerRPCCredentials(bearerToken{token: cfg.AuthToken, secure: secure}),
		grpc.WithKeepaliveParams(keepalive.ClientParameters{
			Time:                5 * time.Minute,
			Timeout:             3 * time.Second,
			PermitWithoutStream: false,
		}),
	)
}

type bearerToken struct {
	token  string
	secure bool
}

func (b bearerToken) GetRequestMetadata(context.Context, ...string) (map[string]string, error) {
	return map[string]string{authMetadataKey: bearerPrefix + b.token}, nil
}

func (b bearerToken) RequireTransportSecurity() bool { return b.secure }
