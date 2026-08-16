namespace APIPlatform.Notification.Repositories;

/// <summary>Dapper row-mapping type for the NotificationUserStates table. See <see cref="NotificationRow"/> for why this exists separately from the public model.</summary>
internal sealed class NotificationUserStateRow
{
    public string UserId { get; set; } = string.Empty;
    public string Application { get; set; } = string.Empty;
    public DateTime? LastReadOnUtc { get; set; }
    public DateTime? LastSyncedOnUtc { get; set; }
    public DateTime UpdatedOnUtc { get; set; }
}
