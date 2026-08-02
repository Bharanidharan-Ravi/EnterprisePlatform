namespace APIPlatform.Foundation.Interfaces;

/// <summary>
/// Abstraction over the current time. Consuming code should always resolve time through this
/// interface rather than calling DateTime.UtcNow directly, so Scheduler, Workflow,
/// Notification, and Sync remain testable and so time can be controlled in the Test Harness.
/// </summary>
public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
