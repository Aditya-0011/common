# Common infrastructure contracts

A single source of truth for inter-service communication schemas.

[![Protobuf](https://img.shields.io/badge/Protobuf-3.21-00add8?style=flat-square)](https://protobuf.dev/)
[![Buf](https://img.shields.io/badge/Buf-Build-244c5a?style=flat-square)](https://buf.build/)
[![gRPC](https://img.shields.io/badge/gRPC-Contracts-244c5a?style=flat-square&logo=grpc)](https://grpc.io/)

## Overview

The `common` repository acts as the central Protocol Buffers (`.proto`) schema registry for the platform. It provides type-safe and validated communication between the REST API gateway and the internal gRPC microservices (`auth`, `manager`, and `wallet`). Centralizing the contracts ensures that the headless portfolio CMS, the identity provider, and the wallet ecosystem stay strictly synchronized across languages.

## Architecture

This section explains the technologies and physical layout of the contracts repository.

- **Schema definition**: Standard Protocol Buffers v3
- **Build tooling**: Uses [Buf](https://buf.build/) for linting, formatting, breaking change detection, and generating client and server stubs
- **Validation**: Integrates `buf.build/go/protovalidate` for defining runtime validation constraints directly inside `.proto` schemas
- **Generated code**: Outputs Go packages and C# projects for polyglot backend microservices

### Project structure

- `contracts/go/`: Generated Go packages (`github.com/Aditya-0011/common/contracts/go`)
- `contracts/go/grpcx/`: Hand-written gRPC plumbing shared by the Go services and the gateway: server setup (recovery, logging, service-token auth, deadline and validation interceptors, health checks, optional TLS, graceful shutdown) and an authenticated client dialer
- `contracts/csharp/`: Generated C# contracts
- `contracts/csharp/Grpcx/`: Hand-written gRPC plumbing for C# services, the counterpart of `grpcx` in Go: `AddGrpcxServer` (Kestrel on `INTERNAL_BIND_IP`, optional TLS, JSON logging, service-token auth, logging/error mapping and validation interceptors, health checks, shutdown timeout), `RpcTimeout`, `IRequestValidator` for custom validation, and `GrpcxClient` for authenticated channels. Shipped in the `Contracts` NuGet package
- `protos/auth/`: Auth service definitions
- `protos/manager/`: Manager service definitions
- `protos/wallet/`: Wallet service definitions
- `protos/buf.gen.yaml`: Buf generation configuration for Go
- `protos/buf.gen.csharp.yaml`: Buf generation configuration for C#

## Features

This section outlines the capabilities of the schema registry.

- **Centralized schema**: Prevents duplication across the microservices infrastructure.
- **Built-in validation**: Defines payload constraints using `protovalidate` annotations.
- **Automated generation**: Compiles contracts into usable code packages for multiple languages.
- **Safe evolution**: Uses `buf breaking` to prevent breaking changes to internal APIs.

## Getting started

This section explains how to compile the schemas locally.

### Prerequisites

- [Buf CLI](https://buf.build/docs/installation) to work with the `.proto` files

### Generating contracts

To compile the schemas locally for Go and C#, navigate to the `protos` directory:

```bash
cd protos
buf generate --template buf.gen.yaml
buf generate --template buf.gen.csharp.yaml
```

To consume the generated Go contracts in a Go service:

```bash
go get github.com/Aditya-0011/common/contracts/go
```

### Local development against unpublished contracts

From the workspace root (the folder containing `Contracts/`, `Gateway/` and `Services/`), run:

```bash
bun run contracts
```

This regenerates the Go and C# code from the local protos and writes a workspace `go.work`.

- **Go services** (Gateway, Auth, Manager) then build against `Contracts/contracts/go` instead of the version pinned in their `go.mod`.
- **Wallet** references `Contracts/contracts/csharp/Contracts.csproj` directly whenever that folder exists and `CI` is not `true`.

Production is unaffected. Each Docker build only receives its own service folder, so it never sees `go.work` or the local C# project, and it uses the published module or package. After pushing contract changes, bump the pins: `go get github.com/Aditya-0011/common/contracts/go@<sha>` in each Go service, and the `Contracts` version in `wallet.csproj`.

### Linting and validation

To check for linting errors or breaking changes:

```bash
cd protos
buf lint
buf breaking --against '.git#branch=main'
```
