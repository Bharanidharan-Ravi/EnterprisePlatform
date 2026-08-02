namespace APIPlatform.Authentication.Events;

/// <summary>Publishes authentication events. NoOp default — Audit/Notification modules register
/// their own handler without any change to the authentication pipeline.</summary>
public interface IAuthenticationEventPublisher
{
    Task PublishAsync(AuthenticationEvent authEvent, CancellationToken cancellationToken = default);
}

public sealed class NoOpAuthenticationEventPublisher : IAuthenticationEventPublisher
{
    public Task PublishAsync(AuthenticationEvent authEvent, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
