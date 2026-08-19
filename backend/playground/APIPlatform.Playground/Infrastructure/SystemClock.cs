using APIPlatform.Foundation.Interfaces;

namespace APIPlatform.Playground.Infrastructure;

/// <summary>
/// Real-time IClock implementation. APIPlatform.Notification and APIPlatform.Database.Migration
/// both require an IClock registration to already be present in the container — they deliberately
/// don't provide one themselves, since it's a shared platform concern each consuming application
/// supplies. Playground supplies it here so its migration wiring (see DatabaseExtensions,
/// DatabaseMigrationController) actually resolves.
/// </summary>
public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
