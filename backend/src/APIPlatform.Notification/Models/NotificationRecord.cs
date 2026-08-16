namespace APIPlatform.Notification.Models;

/// <summary>
/// A persisted notification. Doubles as the module's only read model (repository return type
/// and service return type are the same) — a separate "NotificationDto" would carry the exact
/// same fields, so per the platform's no-unnecessary-abstractions rule there isn't one.
/// </summary>
public sealed record NotificationRecord
{
    public required string Id { get; init; }

    /// <summary>The owning application, e.g. "PROJECT", "IQS", "CRM". Never interpreted by Notification.</summary>
    public required string Application { get; init; }

    /// <summary>Entity context this notification is about, e.g. "PROJECT" (the type). Optional.</summary>
    public string? EntityType { get; init; }

    /// <summary>Entity context this notification is about, e.g. "PRJ001" (the id). Optional.</summary>
    public string? EntityId { get; init; }

    /// <summary>The event/action that raised this notification, e.g. "PROJECT_CREATED". Application-defined.</summary>
    public required string EventType { get; init; }

    public required string Title { get; init; }

    public string? Message { get; init; }

    /// <summary>Opaque application-defined payload (JSON), e.g. deep-link data. Notification stores and
    /// returns this verbatim and never parses or validates it.</summary>
    public string? Data { get; init; }

    /// <summary>The user id who triggered this notification, or null for system-generated ones.</summary>
    public string? CreatedBy { get; init; }

    public required DateTimeOffset CreatedOnUtc { get; init; }
}
