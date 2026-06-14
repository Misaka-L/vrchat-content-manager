using System.Net;

namespace VRChatContentPublisher.TelemetryCore;

public sealed class WebProxyWarper : IWebProxy
{
    public IWebProxy? InnerWebProxy { get; set; }
    
    public Uri? GetProxy(Uri destination)
    {
        return GetWebProxyCore().GetProxy(destination);
    }

    public bool IsBypassed(Uri host)
    {
        return GetWebProxyCore().IsBypassed(host);
    }

    public ICredentials? Credentials { get; set; }

    private IWebProxy GetWebProxyCore()
    {
        return InnerWebProxy ?? HttpClient.DefaultProxy;
    }
}