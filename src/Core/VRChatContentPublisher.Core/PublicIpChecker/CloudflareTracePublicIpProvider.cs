using System.Net;

namespace VRChatContentPublisher.Core.PublicIpChecker;

public sealed class CloudflareTracePublicIpProvider(IHttpClientFactory httpClientFactory)
{
    private const string TraceEndpoint = "https://api.vrchat.cloud/cdn-cgi/trace";

    public async ValueTask<string> GetCurrentPublicIpAsync(CancellationToken cancellationToken = default)
    {
        using var httpClient = httpClientFactory.CreateClient(nameof(CloudflareTracePublicIpProvider));
        using var response = await httpClient.GetAsync(TraceEndpoint, cancellationToken);
        response.EnsureSuccessStatusCode();

        var traceContent = await response.Content.ReadAsStringAsync(cancellationToken);
        var ip = ParseIpFromTrace(traceContent);

        if (!IPAddress.TryParse(ip, out _))
            throw new InvalidOperationException("Cloudflare trace returned an invalid IP address.");

        return ip;
    }

    private static string ParseIpFromTrace(string traceContent)
    {
        foreach (var line in traceContent.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var trimmedLine = line.Trim();
            if (!trimmedLine.StartsWith("ip=", StringComparison.Ordinal))
                continue;

            return trimmedLine[3..].Trim();
        }

        throw new InvalidOperationException("Cloudflare trace response does not contain an ip field.");
    }
}