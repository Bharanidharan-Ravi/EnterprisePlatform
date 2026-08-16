namespace APIPlatform.Notification.Models;

/// <summary>
/// Input to <see cref="Abstractions.INotificationService.CreateAsync"/>. Deliberately excludes
/// <c>Id</c>/<c>CreatedOnUtc</c> — the service generates both (API-generated ids/timestamps, per
/// platform database architecture), a caller never supplies them.
/// </summary>
public sealed record CreateNotificationRequest
{
    public required string Application { get; init; }

    public string? EntityType { get; init; }

    public string? EntityId { get; init; }

    public required string EventType { get; init; }

    public required string Title { get; init; }

    public string? Message { get; init; }

    public string? Data { get; init; }

    public string? CreatedBy { get; init; }

    /// <summary>Who this notification is for/excluded from. Must contain at least one non-exclusion rule.</summary>
    public required IReadOnlyList<NotificationTargetRule> Targets { get; init; }
}
