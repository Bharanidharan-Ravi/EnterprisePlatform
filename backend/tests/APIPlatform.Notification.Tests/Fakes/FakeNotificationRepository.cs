using APIPlatform.Notification.Abstractions;
using APIPlatform.Notification.Models;

namespace APIPlatform.Notification.Tests.Fakes;

/// <summary>Hand-written INotificationRepository test double — no mocking library is used anywhere in this codebase, so tests here follow the same convention.</summary>
internal sealed class FakeNotificationRepository : INotificationRepository
{
    public List<(string Application, string UserId, DateTimeOffset Value)> LastReadOnCalls { get; } = [];
    public List<(string Application, string UserId, DateTimeOffset Value)> LastSyncedOnCalls { get; } = [];
    public List<(string Application, NotificationRecipient Recipient, DateTimeOffset? Since)> CountCalls { get; } = [];
    public List<(string Application, NotificationRecipient Recipient, DateTimeOffset? Since, int Skip, int Take)> ListCalls { get; } = [];

    public NotificationRecord? InsertedNotification { get; private set; }
    public IReadOnlyList<NotificationTargetRule>? InsertedTargets { get; private set; }

    public NotificationUserState? UserState { get; set; }
    public int CountResult { get; set; }
    public IReadOnlyList<NotificationRecord> ListResult { get; set; } = [];

    public Task<NotificationRecord> InsertAsync(NotificationRecord notification, IReadOnlyList<NotificationTargetRule> targets, CancellationToken cancellationToken = default)
    {
        InsertedNotification = notification;
        InsertedTargets = targets;
        return Task.FromResult(notification);
    }

    public Task<IReadOnlyList<NotificationRecord>> ListForRecipientAsync(string application, NotificationRecipient recipient, DateTimeOffset? since, int skip, int take, CancellationToken cancellationToken = default)
    {
        ListCalls.Add((application, recipient, since, skip, take));
        return Task.FromResult(ListResult);
    }

    public Task<int> CountForRecipientAsync(string application, NotificationRecipient recipient, DateTimeOffset? since, CancellationToken cancellationToken = default)
    {
        CountCalls.Add((application, recipient, since));
        return Task.FromResult(CountResult);
    }

    public Task<IReadOnlyList<NotificationRecord>> ListForEntityAsync(string application, string entityType, string entityId, int skip, int take, CancellationToken cancellationToken = default) =>
        Task.FromResult(ListResult);

    public Task<NotificationUserState?> GetUserStateAsync(string application, string userId, CancellationToken cancellationToken = default) =>
        Task.FromResult(UserState);

    public Task SetLastReadOnAsync(string application, string userId, DateTimeOffset readOnUtc, CancellationToken cancellationToken = default)
    {
        LastReadOnCalls.Add((application, userId, readOnUtc));
        return Task.CompletedTask;
    }

    public Task SetLastSyncedOnAsync(string application, string userId, DateTimeOffset syncedOnUtc, CancellationToken cancellationToken = default)
    {
        LastSyncedOnCalls.Add((application, userId, syncedOnUtc));
        return Task.CompletedTask;
    }
}
