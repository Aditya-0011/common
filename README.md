<div align="center">

# Common Infrastructure Contracts
*Single source of truth for inter-service communication schema*

[![Protobuf](https://img.shields.io/badge/Protobuf-3.21-00add8?style=flat-square)](https://protobuf.dev/)
[![Buf](https://img.shields.io/badge/Buf-Build-244c5a?style=flat-square)](https://buf.build/)
[![gRPC](https://img.shields.io/badge/gRPC-Contracts-244c5a?style=flat-square&logo=grpc)](https://grpc.io/)

⭐ If you find this repository useful, star it on GitHub!

[Overview](#overview) • [Architecture & Tech Stack](#architecture--tech-stack) • [Features](#features) • [Getting Started](#getting-started) • [Polyrepo Local Setup](#polyrepo-local-setup)

</div>

## Overview

The `common` repository acts as the central Protocol Buffers (`.proto`) definitions and generated contracts for all microservices in the infrastructure workspace. It ensures type-safe and validated communication between services such as `auth` and `manager` by serving as the definitive schema repository.

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
│   ├── auth/      # Auth service definitions
│   └── manager/   # Manager service definitions
├── buf.gen.yaml   # Buf generation configuration for Go
├── buf.gen.csharp.yaml # Buf generation configuration for C#
└── README.md      # This file
```

## Features

- 📜 **Centralized Schema**: A single repository for all `.proto` files, preventing duplication across microservices.
- 🛡️ **Built-in Validation**: Defines payload constraints (e.g., minimum length, string formats) using `protovalidate` annotations.
- ⚙️ **Automated Generation**: Easy contract compilation for both Go and C# ecosystems.
- 🚦 **Safe Evolution**: Uses `buf breaking` to prevent accidental breaking changes to APIs.

## Getting Started

### Prerequisites

- [Buf CLI](https://buf.build/docs/installation) is required to work with the `.proto` files.

### Installation

Clone the repository:
```bash
git clone https://github.com/Aditya-0011/common.git common
cd common
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

## Polyrepo Local Setup

This project uses a polyrepo architecture. Services like `auth` and `manager` live in their own repositories but rely on these common contracts. 

To consume the contracts in a Go service:
```bash
go get github.com/Aditya-0011/common/contracts/go
```

> [!IMPORTANT]
> When developing locally, keep all repositories (`common`, `auth`, `manager`) in the same parent directory. You can use Go Workspaces (`go.work`) in the parent directory to safely resolve the local `common` module without polluting the `go.mod` files of the microservices.

Example local setup:
```text
infrastructure/
├── common/
├── auth/
├── manager/
└── go.work # Points to ./common, ./auth, ./manager
```
