namespace APIPlatform.Notification.Models;

/// <summary>
/// Who a <see cref="NotificationTargetRule"/> refers to. Persisted as TINYINT — treat these as
/// frozen values (append-only) since existing rows encode them numerically.
/// </summary>
public enum NotificationTargetKind : byte
{
    /// <summary>Every user of the application. <see cref="NotificationTargetRule.Value"/> is null.</summary>
    All = 0,

    /// <summary>A single user, identified by <see cref="NotificationTargetRule.Value"/> (the user id).</summary>
    User = 1,

    /// <summary>
    /// A group/team, identified by <see cref="NotificationTargetRule.Value"/> (an application-defined
    /// group code). Notification never resolves what a group contains — see
    /// <see cref="NotificationRecipient"/> and the module README.
    /// </summary>
    Group = 2
}
