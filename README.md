# Common Infrastructure Contracts

A single source of truth for inter-service communication schema.

[![Protobuf](https://img.shields.io/badge/Protobuf-3.21-00add8?style=flat-square)](https://protobuf.dev/)
[![Buf](https://img.shields.io/badge/Buf-Build-244c5a?style=flat-square)](https://buf.build/)
[![gRPC](https://img.shields.io/badge/gRPC-Contracts-244c5a?style=flat-square&logo=grpc)](https://grpc.io/)

## Overview

The `common` repository acts as the central Protocol Buffers (`.proto`) schema registry for the developer platform. It ensures type-safe and validated communication between the REST API Gateway and the internal gRPC microservices (`auth` and `manager`). By centralizing the contracts here, the platform guarantees that the Headless Portfolio CMS and the Identity Provider stay strictly synchronized.

## Architecture & Tech Stack

- **Schema Definition**: Standard Protocol Buffers v3.
- **Build Tooling**: Uses [Buf](https://buf.build/) for linting, formatting, breaking change detection, and generating client/server stubs in multiple languages.
- **Validation**: Integrates `buf.build/go/protovalidate` for defining runtime validation constraints directly inside `.proto` schemas.
- **Generated Code**: Currently outputs Go packages (for backend microservices) and C# contracts.

### Project Structure

```text
.
├── contracts/     # Generated source code ready for consumption
│   ├── csharp/    # Generated C# contracts
│   └── go/        # Generated Go packages (github.com/Aditya-0011/common/contracts/go)
├── protos/        # Core Protocol Buffer source files, separated by domain
│   ├── auth/      # Auth service definitions (Identity Provider)
│   └── manager/   # Manager service definitions (Portfolio CMS)
├── buf.gen.yaml   # Buf generation configuration for Go
├── buf.gen.csharp.yaml # Buf generation configuration for C#
└── README.md      # This file
```

## Features

- 📜 **Centralized Schema**: A single repository for all `.proto` files, preventing duplication across the microservices infrastructure.
- 🛡️ **Built-in Validation**: Defines payload constraints (e.g., minimum length, string formats) using `protovalidate` annotations.
- ⚙️ **Automated Generation**: Easy contract compilation for both Go and C# ecosystems.
- 🚦 **Safe Evolution**: Uses `buf breaking` to prevent accidental breaking changes to the internal APIs.

## Getting Started

### Prerequisites

- [Buf CLI](https://buf.build/docs/installation) is required to work with the `.proto` files.

### Polyrepo Local Setup

This project uses a polyrepo architecture. Services like `auth`, `manager`, and `gateway` rely on these common contracts. To develop locally without issues, clone all repositories side-by-side into the same parent directory so that relative paths resolve correctly.

Example local setup:
```text
infrastructure/
├── common/
├── auth/
├── manager/
└── gateway/
```

To consume the contracts in a remote Go service:
```bash
go get github.com/Aditya-0011/common/contracts/go
```

### Generating Contracts

To compile the schemas locally, navigate to the `protos` directory:
```bash
cd protos

# Generate Go code
buf generate --template buf.gen.yaml

# Generate C# code
buf generate --template buf.gen.csharp.yaml
```

### Linting and Validation

To check for linting errors or breaking changes:
```bash
cd protos

# Run the linter
buf lint

# Check for breaking changes against the main branch
buf breaking --against '.git#branch=main'
```
