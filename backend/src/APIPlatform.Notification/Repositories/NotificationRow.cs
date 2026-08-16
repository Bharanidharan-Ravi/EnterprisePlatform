namespace APIPlatform.Notification.Repositories;

/// <summary>
/// Dapper row-mapping type for the Notifications table. Kept separate from the public
/// <see cref="Models.NotificationRecord"/> because timestamps round-trip through the database as
/// plain UTC <see cref="DateTime"/> (DATETIME2/TIMESTAMP have no offset component, so no offset
/// survives the round trip) while the public contract exposes the richer <see cref="DateTimeOffset"/>
/// consistently with <c>IClock</c>. Requires public settable properties and a parameterless
/// constructor for Dapper's default materializer.
/// </summary>
internal sealed class NotificationRow
{
    public string Id { get; set; } = string.Empty;
    public string Application { get; set; } = string.Empty;
    public string? EntityType { get; set; }
    public string? EntityId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Message { get; set; }
    public string? Data { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime CreatedOnUtc { get; set; }
}
