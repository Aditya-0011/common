# Common Infrastructure Contracts

> [!NOTE]
> This repository contains the central Protocol Buffers (`.proto`) definitions and generated contracts for all microservices in the infrastructure workspace.

The `common` repository acts as the single source of truth for inter-service communication schema. It uses [Buf](https://buf.build/) for linting, formatting, and generating client/server stubs in multiple languages (currently Go and C#).

## Repository Structure

- **`protos/`**: The core Protocol Buffer source files, separated by domain (e.g., `auth/`, `manager/`).
- **`contracts/`**: The generated source code ready for consumption by other services.
  - `go/`: Contains the generated Go packages (e.g., `github.com/Aditya-0011/common/contracts/go`).
  - `csharp/`: Contains the generated C# contracts.

## Available Services

- **Auth Service**: Definitions for authentication and authorization.
- **Manager Service**: Definitions for user and portfolio management.

## Getting Started

### Prerequisites

You will need the `buf` CLI to work with the `.proto` files.

> [!TIP]
> You can install Buf via Homebrew (`brew install bufbuild/buf/buf`), download it via `npm` or download the binary from their [releases page](https://github.com/bufbuild/buf/releases).

### Generating Contracts

To compile the schemas and generate the Go and C# contracts locally, navigate to the `protos` directory and run:

```bash
cd protos

# Generate Go code
buf generate --template buf.gen.yaml

# Generate C# code
buf generate --template buf.gen.csharp.yaml
```

### Linting and Validation

We use `protovalidate` to enforce runtime validation rules directly inside our schemas. To check for linting errors or breaking changes:

```bash
cd protos

# Run the linter
buf lint

# Check for breaking changes against the main branch
buf breaking --against '.git#branch=main'
```

## Consuming the Contracts

### Go

Require this module in your Go project:

```bash
go get github.com/Aditya-0011/common/contracts/go
```

Then, import the generated packages in your microservices:

```go
import "github.com/Aditya-0011/common/contracts/go/auth"
```

> [!IMPORTANT]
> If you are working locally across repositories and making changes to the protos, you may need a `replace` directive in your consumer's `go.mod` file to point to the local filesystem path (e.g., `replace github.com/Aditya-0011/common/contracts/go => ../common/contracts/go`).
