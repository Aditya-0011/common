using System.Security.Cryptography;
using System.Text;
using Grpc.Core;
using Grpc.Core.Interceptors;

namespace Contracts.Grpcx;

public sealed class ServiceAuthInterceptor(ServiceOptions options) : Interceptor
{
    internal const string MetadataKey = "authorization";
    internal const string BearerPrefix = "Bearer ";
    private const string HealthPrefix = "/grpc.health.v1.Health/";

    private readonly byte[] _expected = SHA256.HashData(Encoding.UTF8.GetBytes(options.AuthToken));

    public override Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request,
        ServerCallContext context,
        UnaryServerMethod<TRequest, TResponse> continuation)
    {
        if (context.Method.StartsWith(HealthPrefix, StringComparison.Ordinal))
            return continuation(request, context);

        var header = context.RequestHeaders.GetValue(MetadataKey);
        if (header is null || !header.StartsWith(BearerPrefix, StringComparison.Ordinal))
            throw new RpcException(new Status(StatusCode.PermissionDenied, "missing service credentials"));

        var actual = SHA256.HashData(Encoding.UTF8.GetBytes(header[BearerPrefix.Length..]));
        if (!CryptographicOperations.FixedTimeEquals(actual, _expected))
            throw new RpcException(new Status(StatusCode.PermissionDenied, "invalid service credentials"));

        return continuation(request, context);
    }
}
