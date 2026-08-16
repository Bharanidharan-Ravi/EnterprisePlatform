using APIPlatform.Foundation.Results;
using APIPlatform.Notification.Models;

namespace APIPlatform.Notification.Abstractions;

/// <summary>
/// Application-facing entry point for the notification engine. Thin orchestration over
/// <see cref="INotificationRepository"/>: validates input, generates ids/timestamps, and wraps
/// outcomes in the platform's standard <see cref="Result{T}"/>/<see cref="OperationResult"/> envelopes.
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Creates a notification. Conceptually: Application + Entity + Event + one or more
    /// Target/Exclude rules. The module decides how to persist and later resolve this — the
    /// caller only expresses what happened and who it's for.
    /// </summary>
    Task<Result<NotificationRecord>> CreateAsync(CreateNotificationRequest request, CancellationToken cancellationToken = default);

    /// <summary>Paged inbox for one recipient within one application, newest first.</summary>
    Task<PagedResult<NotificationRecord>> GetNotificationsAsync(
        string application,
        NotificationRecipient recipient,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>Activity feed for a specific entity (e.g. PROJECT/PRJ001), independent of recipient.</summary>
    Task<PagedResult<NotificationRecord>> GetNotificationsForEntityAsync(
        string application,
        string entityType,
        string entityId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>Count of notifications matching <paramref name="recipient"/> created since their last read.</summary>
    Task<int> GetUnreadCountAsync(string application, NotificationRecipient recipient, CancellationToken cancellationToken = default);

    /// <summary>Marks everything up to <paramref name="upToUtc"/> (default: now) as read for this user/application.</summary>
    Task<OperationResult> MarkAsReadAsync(string application, string userId, DateTimeOffset? upToUtc = null, CancellationToken cancellationToken = default);

    /// <summary>Records that the client synchronized/polled, without affecting read state.</summary>
    Task<OperationResult> MarkAsSyncedAsync(string application, string userId, DateTimeOffset? atUtc = null, CancellationToken cancellationToken = default);
}
