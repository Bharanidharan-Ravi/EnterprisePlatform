namespace APIPlatform.Notification.Models;

/// <summary>
/// The reader whose notifications/unread state is being queried. <see cref="GroupCodes"/> is
/// resolved by the calling application (from its own RBAC/org/team model) and supplied here —
/// Notification never resolves group membership itself. This is what keeps the module
/// independent of IQS/Nucleus/CRM/Project/Ticketing/HRMS's own notion of "group": Notification
/// only ever compares opaque codes, it never looks up what a group contains.
/// </summary>
public sealed record NotificationRecipient
{
    public required string UserId { get; init; }

    public IReadOnlyList<string> GroupCodes { get; init; } = Array.Empty<string>();

    public static NotificationRecipient For(string userId, IReadOnlyList<string>? groupCodes = null) =>
        new() { UserId = userId, GroupCodes = groupCodes ?? Array.Empty<string>() };
}
