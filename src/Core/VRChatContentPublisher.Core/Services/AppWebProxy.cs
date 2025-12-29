using System.Net;
using VRChatContentPublisher.Core.Settings;
using VRChatContentPublisher.Core.Settings.Models;

namespace VRChatContentPublisher.Core.Services;

public sealed class AppWebProxy(IWritableOptions<AppSettings> appSettings) : IWebProxy
{
    private CustomWebProxyInstance? _customWebProxyInstance;

    public Uri? GetProxy(Uri destination)
    {
        return GetProxyCore()?.GetProxy(destination);
    }

    public bool IsBypassed(Uri host)
    {
        return GetProxyCore()?.IsBypassed(host) ?? true;
    }

    private IWebProxy? GetProxyCore()
    {
        switch (appSettings.Value.HttpProxySettings)
        {
            case AppHttpProxySettings.NoProxy:
                return null;
            case AppHttpProxySettings.CustomProxy:
                return GetCustomWebProxy();
            default:
            case AppHttpProxySettings.SystemProxy:
                return WebRequest.GetSystemWebProxy();
        }
    }

    private IWebProxy GetCustomWebProxy()
    {
        var customProxyUri = appSettings.Value.HttpProxyUri;
        if (customProxyUri is null)
            return WebRequest.GetSystemWebProxy();

        if (_customWebProxyInstance?.CustomProxyUri == customProxyUri)
            return _customWebProxyInstance.CustomProxy;

        var webProxy = new WebProxy(customProxyUri);
        _customWebProxyInstance = new CustomWebProxyInstance(webProxy, customProxyUri);

        return webProxy;
    }

    private record CustomWebProxyInstance(IWebProxy CustomProxy, Uri CustomProxyUri);

    public ICredentials? Credentials { get; set; }
}