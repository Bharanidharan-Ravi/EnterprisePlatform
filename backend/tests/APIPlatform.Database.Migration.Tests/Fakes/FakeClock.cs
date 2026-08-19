using APIPlatform.Foundation.Interfaces;

namespace APIPlatform.Database.Migration.Tests.Fakes;

internal sealed class FakeClock : IClock
{
    public DateTimeOffset UtcNow { get; set; } = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
}
