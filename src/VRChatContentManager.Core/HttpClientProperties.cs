using System.Net.Http.Headers;

namespace VRChatContentManager.Core;

public static class HttpClientProperties
{
    public static ProductInfoHeaderValue GetUserAgent()
    {
        return new ProductInfoHeaderValue(new ProductHeaderValue("VRChatContentManager", "snapshot"));
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