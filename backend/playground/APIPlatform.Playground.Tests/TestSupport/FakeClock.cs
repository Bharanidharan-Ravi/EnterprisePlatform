using APIPlatform.Foundation.Interfaces;

namespace APIPlatform.Playground.Tests.TestSupport;

internal sealed class FakeClock : IClock
{
    public DateTimeOffset UtcNow { get; set; } = new(2026, 8, 21, 12, 0, 0, TimeSpan.Zero);
}
