using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ProtoValidate;

namespace Contracts.Grpcx;

public static class GrpcxServer
{
    public static readonly TimeSpan ShutdownTimeout = TimeSpan.FromSeconds(10);

    public static ServiceOptions AddGrpcxServer(this WebApplicationBuilder builder, int defaultPort)
    {
        var options = ServiceOptions.FromEnvironment(defaultPort);

        builder.Logging.ClearProviders();
        builder.Logging.AddJsonConsole(o => o.UseUtcTimestamp = true);

        builder.WebHost.ConfigureKestrel(kestrel =>
        {
            kestrel.Listen(options.BindIp, options.Port, listen =>
            {
                listen.Protocols = HttpProtocols.Http2;
                if (options.TlsCertFile is not null)
                    listen.UseHttps(X509Certificate2.CreateFromPemFile(options.TlsCertFile, options.TlsKeyFile));
            });
        });

        builder.Services.Configure<HostOptions>(o => o.ShutdownTimeout = ShutdownTimeout);
        builder.Services.AddSingleton(options);
        builder.Services.AddSingleton<Validator>();

        builder.Services.AddGrpc(grpc =>
        {
            grpc.Interceptors.Add<ServiceAuthInterceptor>();
            grpc.Interceptors.Add<LoggingInterceptor>();
            grpc.Interceptors.Add<ValidationInterceptor>();
        });
        builder.Services
            .AddGrpcHealthChecks(o => o.UseHealthChecksCache = false)
            .AddCheck("self", () => HealthCheckResult.Healthy());

        return options;
    }

    public static IEndpointRouteBuilder MapGrpcxHealth(this IEndpointRouteBuilder app)
    {
        app.MapGrpcHealthChecksService();
        return app;
    }
}
