namespace APIPlatform.Notification.Models;

/// <summary>
/// A user's read/sync cursor for one application — one row per (user, application) regardless
/// of how many notifications exist, so unread-count/read-state stays O(1) storage per user
/// rather than materializing a row per notification per user.
/// </summary>
public sealed record NotificationUserState
{
    public required string UserId { get; init; }

    public required string Application { get; init; }

    /// <summary>Notifications created at or before this time are considered read. Null means
    /// nothing has ever been marked read (everything targeted at the user is unread).</summary>
    public DateTimeOffset? LastReadOnUtc { get; init; }

    /// <summary>Last time the client synchronized/polled for this application. Distinct from
    /// <see cref="LastReadOnUtc"/> — a client can observe new notifications without the user
    /// having acknowledged/read them yet.</summary>
    public DateTimeOffset? LastSyncedOnUtc { get; init; }

    public required DateTimeOffset UpdatedOnUtc { get; init; }
}
