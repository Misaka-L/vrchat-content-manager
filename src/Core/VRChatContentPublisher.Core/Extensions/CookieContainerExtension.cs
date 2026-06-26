using System.Net;

namespace VRChatContentPublisher.Core.Extensions;

public static class CookieContainerExtension
{
    public static void SetAllCookiesExpired(this CookieContainer cookieContainer)
    {
        foreach (Cookie cookie in cookieContainer.GetAllCookies())
        {
            cookie.Expired = true;
        }
    }
}