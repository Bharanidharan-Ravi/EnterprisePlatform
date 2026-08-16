namespace APIPlatform.Notification.Models;

/// <summary>
/// A single target or exclusion rule attached to a notification at creation time — e.g.
/// "Target = GROUP/PROJECT_TEAM" or "Exclude = USER/USER007". A notification carries one or
/// more of these instead of one row per eligible recipient; who actually matches is resolved
/// at read time against the caller-supplied <see cref="NotificationRecipient"/>.
/// </summary>
public sealed record NotificationTargetRule
{
    public required NotificationTargetKind Kind { get; init; }

    /// <summary>The user id or group code this rule refers to. Must be null when <see cref="Kind"/>
    /// is <see cref="NotificationTargetKind.All"/>, and non-empty otherwise.</summary>
    public string? Value { get; init; }

    /// <summary>True if this rule excludes rather than includes the given user/group.</summary>
    public bool IsExclusion { get; init; }

    public static NotificationTargetRule TargetAll() => new() { Kind = NotificationTargetKind.All };

    public static NotificationTargetRule TargetUser(string userId) =>
        new() { Kind = NotificationTargetKind.User, Value = userId };

    public static NotificationTargetRule TargetGroup(string groupCode) =>
        new() { Kind = NotificationTargetKind.Group, Value = groupCode };

    public static NotificationTargetRule ExcludeUser(string userId) =>
        new() { Kind = NotificationTargetKind.User, Value = userId, IsExclusion = true };

    public static NotificationTargetRule ExcludeGroup(string groupCode) =>
        new() { Kind = NotificationTargetKind.Group, Value = groupCode, IsExclusion = true };
}
