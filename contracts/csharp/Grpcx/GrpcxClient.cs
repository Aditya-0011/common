using System.Security.Cryptography.X509Certificates;
using Grpc.Core;
using Grpc.Net.Client;

namespace Contracts.Grpcx;

public sealed record ClientOptions(string Address, string AuthToken, string? TlsCaFile = null);

public static class GrpcxClient
{
    public static GrpcChannel CreateChannel(ClientOptions options)
    {
        if (string.IsNullOrEmpty(options.Address))
            throw new ArgumentException("service address is empty", nameof(options));
        if (options.AuthToken.Length < ServiceOptions.MinTokenLength)
            throw new ArgumentException(
                $"auth token must be at least {ServiceOptions.MinTokenLength} characters", nameof(options));

        var secure = options.TlsCaFile is not null;
        var handler = new SocketsHttpHandler
        {
            PooledConnectionIdleTimeout = TimeSpan.FromMinutes(5),
            KeepAlivePingDelay = TimeSpan.FromMinutes(5),
            KeepAlivePingTimeout = TimeSpan.FromSeconds(3),
            EnableMultipleHttp2Connections = true
        };

        if (secure)
        {
            var ca = X509CertificateLoader.LoadCertificateFromFile(options.TlsCaFile!);
            handler.SslOptions.RemoteCertificateValidationCallback = (_, cert, chain, errors) =>
            {
                if (cert is null || chain is null) return false;
                if (errors == System.Net.Security.SslPolicyErrors.None) return true;
                chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
                chain.ChainPolicy.CustomTrustStore.Add(ca);
                return chain.Build(new X509Certificate2(cert));
            };
        }

        var scheme = secure ? "https://" : "http://";
        var address = options.Address.Contains("://", StringComparison.Ordinal)
            ? options.Address
            : scheme + options.Address;

        var token = options.AuthToken;
        var credentials = CallCredentials.FromInterceptor((_, metadata) =>
        {
            metadata.Add(ServiceAuthInterceptor.MetadataKey, ServiceAuthInterceptor.BearerPrefix + token);
            return Task.CompletedTask;
        });

        return GrpcChannel.ForAddress(address, new GrpcChannelOptions
        {
            HttpHandler = handler,
            Credentials = secure
                ? ChannelCredentials.Create(ChannelCredentials.SecureSsl, credentials)
                : ChannelCredentials.Create(ChannelCredentials.Insecure, credentials),
            UnsafeUseInsecureChannelCallCredentials = !secure
        });
    }
}
