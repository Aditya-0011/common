# Common infrastructure contracts

A single source of truth for inter-service communication schemas.

[![Protobuf](https://img.shields.io/badge/Protobuf-3.21-00add8?style=flat-square)](https://protobuf.dev/)
[![Buf](https://img.shields.io/badge/Buf-Build-244c5a?style=flat-square)](https://buf.build/)
[![gRPC](https://img.shields.io/badge/gRPC-Contracts-244c5a?style=flat-square&logo=grpc)](https://grpc.io/)

## Overview

The `common` repository acts as the central Protocol Buffers (`.proto`) schema registry for the platform. It provides type-safe and validated communication between the REST API gateway and the internal gRPC microservices (`auth` and `manager`). Centralizing the contracts ensures that the headless portfolio CMS and the identity provider stay strictly synchronized.

## Architecture

This section explains the technologies and physical layout of the contracts repository.

- **Schema definition**: Standard Protocol Buffers v3
- **Build tooling**: Uses [Buf](https://buf.build/) for linting, formatting, breaking change detection, and generating client and server stubs
- **Validation**: Integrates `buf.build/go/protovalidate` for defining runtime validation constraints directly inside `.proto` schemas
- **Generated code**: Outputs Go packages for the backend microservices

### Project structure

- `contracts/go/`: Generated Go packages (`github.com/Aditya-0011/common/contracts/go`)
- `protos/auth/`: Auth service definitions
- `protos/manager/`: Manager service definitions
- `buf.gen.yaml`: Buf generation configuration for Go

## Features

This section outlines the capabilities of the schema registry.

- **Centralized schema**: Prevents duplication across the microservices infrastructure.
- **Built-in validation**: Defines payload constraints using `protovalidate` annotations.
- **Automated generation**: Compiles contracts into usable code packages.
- **Safe evolution**: Uses `buf breaking` to prevent breaking changes to internal APIs.

## Getting started

This section explains how to compile the schemas locally.

### Prerequisites

- [Buf CLI](https://buf.build/docs/installation) to work with the `.proto` files

### Generating contracts

To compile the schemas locally for Go, navigate to the `protos` directory:

```bash
cd protos
buf generate --template buf.gen.yaml
```

To consume the generated contracts in a Go service:

```bash
go get github.com/Aditya-0011/common/contracts/go
```

### Linting and validation

To check for linting errors or breaking changes:

```bash
cd protos
buf lint
buf breaking --against '.git#branch=main'
```
