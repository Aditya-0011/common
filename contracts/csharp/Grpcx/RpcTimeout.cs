using Grpc.Core;

namespace Contracts.Grpcx;

public static class RpcTimeout
{
    public static CancellationTokenSource CreateTimeout(this ServerCallContext context, ServiceOptions options)
    {
        var cts = CancellationTokenSource.CreateLinkedTokenSource(context.CancellationToken);
        cts.CancelAfter(options.Timeout);
        return cts;
    }
}
