namespace VRChatContentPublisher.Core;

public class InspectorHttpHandler(InspectorHttpHandlerDelegate inspectorFunc) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken
    )
    {
        var response = await base.SendAsync(request, cancellationToken);
        await inspectorFunc(response);
        return response;
    }
}

public delegate Task InspectorHttpHandlerDelegate(HttpResponseMessage response);