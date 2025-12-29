using System.Net.Http.Headers;
using VRChatContentPublisher.Core.Utils;

namespace VRChatContentPublisher.Core;

public static class HttpClientProperties
{
    private static string GetAppVersion() => AppVersionUtils.GetAppVersion() + "+" + AppVersionUtils.GetAppCommitHash();
    
    public static ProductInfoHeaderValue GetUserAgent()
    {
        return new ProductInfoHeaderValue(new ProductHeaderValue("VRChatContentManager", GetAppVersion()));
    }

    public static ProductInfoHeaderValue GetUserAgentComment()
    {
        return new ProductInfoHeaderValue("(author: Misaka-L<lipww1234@foxmail.com>)");
    }
    
    public static HttpClient AddUserAgent(this HttpClient client)
    {
        client.DefaultRequestHeaders.UserAgent.Add(GetUserAgent());
        client.DefaultRequestHeaders.UserAgent.Add(GetUserAgentComment());
        return client;
    }
}