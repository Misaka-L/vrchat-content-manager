using MessagePipe;
using VRChatContentPublisher.Core.Events.UserSession;
using VRChatContentPublisher.Core.Services.UserSession;

namespace VRChatContentPublisher.Core.Services.PublishTask.ContentPublisher;

public sealed class EnsureSessionValidScope : IDisposable
{
    private readonly CancellationTokenSource _cts;
    private readonly IDisposable _subscription;

    public EnsureSessionValidScope(
        string userNameOrEmail,
        ISubscriber<SessionStateChangedEvent> subscriber,
        CancellationToken taskCancellationToken = default
    )
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(taskCancellationToken);
        _subscription = subscriber.Subscribe(args =>
        {
            if (args.UserNameOrEmail != userNameOrEmail)
                return;

            if (args.SessionState != UserSessionState.LoggedIn)
            {
                _cts.Cancel();
            }
        });
    }

    public CancellationToken CancellationToken => _cts.Token;

    public void Dispose()
    {
        _subscription.Dispose();
        _cts.Cancel();
    }
}