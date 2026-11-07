using System.Diagnostics;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.Extensions.Logging;

namespace Contracts.Grpcx;

public sealed class LoggingInterceptor(ILogger<LoggingInterceptor> logger) : Interceptor
{
    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request,
        ServerCallContext context,
        UnaryServerMethod<TRequest, TResponse> continuation)
    {
        var started = Stopwatch.GetTimestamp();

        try
        {
            var response = await continuation(request, context);
            Log(LogLevel.Information, context, StatusCode.OK, started);
            return response;
        }
        catch (RpcException ex)
        {
            Log(LevelFor(ex.StatusCode), context, ex.StatusCode, started, ex.Status.Detail);
            throw;
        }
        catch (OperationCanceledException)
        {
            Log(LogLevel.Warning, context, StatusCode.DeadlineExceeded, started);
            throw new RpcException(new Status(StatusCode.DeadlineExceeded, "Deadline exceeded"));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "gRPC request {Method} {Status} {DurationMs}",
                context.Method, StatusCode.Internal, Stopwatch.GetElapsedTime(started).TotalMilliseconds);
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
    }

    private void Log(LogLevel level, ServerCallContext context, StatusCode code, long started, string? error = null)
    {
        logger.Log(level, "gRPC request {Method} {Status} {DurationMs} {Error}",
            context.Method, code, Stopwatch.GetElapsedTime(started).TotalMilliseconds, error);
    }

    private static LogLevel LevelFor(StatusCode code) => code switch
    {
        StatusCode.OK or StatusCode.Cancelled or StatusCode.InvalidArgument or StatusCode.NotFound
            or StatusCode.AlreadyExists or StatusCode.Unauthenticated => LogLevel.Information,
        StatusCode.DeadlineExceeded or StatusCode.PermissionDenied or StatusCode.ResourceExhausted
            or StatusCode.FailedPrecondition or StatusCode.Aborted or StatusCode.OutOfRange
            or StatusCode.Unavailable => LogLevel.Warning,
        _ => LogLevel.Error
    };
}
