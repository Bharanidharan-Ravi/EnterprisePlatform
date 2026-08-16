using APIPlatform.Notification.Models;

namespace APIPlatform.Notification.Abstractions;

/// <summary>
/// Data access for Notification's three tables. Kept small and responsibility-focused —
/// recipient/group resolution is never performed here (or anywhere in this module); callers
/// supply the recipient's identity and group codes already resolved.
/// </summary>
public interface INotificationRepository
{
    /// <summary>Inserts a notification and all of its target/exclusion rules atomically. Fails the
    /// whole operation if any part fails — a notification with no targets is never persisted.</summary>
    Task<NotificationRecord> InsertAsync(
        NotificationRecord notification,
        IReadOnlyList<NotificationTargetRule> targets,
        CancellationToken cancellationToken = default);

    /// <summary>Lists notifications for <paramref name="application"/> that match <paramref name="recipient"/>
    /// (directly, via ALL, or via one of their groups) and are not excluded, newest first.</summary>
    Task<IReadOnlyList<NotificationRecord>> ListForRecipientAsync(
        string application,
        NotificationRecipient recipient,
        DateTimeOffset? since,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    /// <summary>Same matching rules as <see cref="ListForRecipientAsync"/>, count only.</summary>
    Task<int> CountForRecipientAsync(
        string application,
        NotificationRecipient recipient,
        DateTimeOffset? since,
        CancellationToken cancellationToken = default);

    /// <summary>Notification history for a specific entity (e.g. PROJECT/PRJ001), regardless of
    /// recipient — an activity feed, not a per-user inbox.</summary>
    Task<IReadOnlyList<NotificationRecord>> ListForEntityAsync(
        string application,
        string entityType,
        string entityId,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    Task<NotificationUserState?> GetUserStateAsync(
        string application,
        string userId,
        CancellationToken cancellationToken = default);

    /// <summary>Sets the user's read cursor, creating the state row on first use. Update-first with
    /// insert-fallback (no MERGE, for SQL Server/HANA portability) — see README for the concurrency note.</summary>
    Task SetLastReadOnAsync(
        string application,
        string userId,
        DateTimeOffset readOnUtc,
        CancellationToken cancellationToken = default);

    /// <summary>Sets the user's sync cursor, creating the state row on first use. Independent of
    /// <see cref="SetLastReadOnAsync"/> — sync and read are distinct pieces of state.</summary>
    Task SetLastSyncedOnAsync(
        string application,
        string userId,
        DateTimeOffset syncedOnUtc,
        CancellationToken cancellationToken = default);
}
