using System.Net;

namespace Contracts.Grpcx;

public sealed record ServiceOptions(
    IPAddress BindIp,
    int Port,
    string AuthToken,
    string? TlsCertFile,
    string? TlsKeyFile,
    TimeSpan Timeout)
{
    public const int MinTokenLength = 32;

    public static ServiceOptions FromEnvironment(int defaultPort)
    {
        var errors = new List<string>();

        if (!IPAddress.TryParse(Environment.GetEnvironmentVariable("INTERNAL_BIND_IP"), out var bindIp))
            errors.Add("INTERNAL_BIND_IP must be set to the private IP the gateway connects to");

        var port = defaultPort;
        var portRaw = Environment.GetEnvironmentVariable("PORT");
        if (!string.IsNullOrEmpty(portRaw) && (!int.TryParse(portRaw, out port) || port is <= 0 or > 65535))
            errors.Add("PORT must be a valid port number");

        var token = Environment.GetEnvironmentVariable("INTERNAL_AUTH_TOKEN") ?? "";
        if (token.Length < MinTokenLength)
            errors.Add($"INTERNAL_AUTH_TOKEN must be set and at least {MinTokenLength} characters");

        var cert = NullIfEmpty(Environment.GetEnvironmentVariable("INTERNAL_TLS_CERT"));
        var key = NullIfEmpty(Environment.GetEnvironmentVariable("INTERNAL_TLS_KEY"));
        if ((cert is null) != (key is null))
            errors.Add("INTERNAL_TLS_CERT and INTERNAL_TLS_KEY must be set together");

        var timeout = TimeSpan.FromSeconds(5);
        var timeoutRaw = Environment.GetEnvironmentVariable("RPC_TIMEOUT_MS");
        if (!string.IsNullOrEmpty(timeoutRaw))
        {
            if (int.TryParse(timeoutRaw, out var ms) && ms > 0) timeout = TimeSpan.FromMilliseconds(ms);
            else errors.Add("RPC_TIMEOUT_MS must be a positive integer");
        }

        if (errors.Count > 0)
            throw new InvalidOperationException(string.Join("; ", errors));

        return new ServiceOptions(bindIp!, port, token, cert, key, timeout);
    }

    private static string? NullIfEmpty(string? value) => string.IsNullOrEmpty(value) ? null : value;
}
