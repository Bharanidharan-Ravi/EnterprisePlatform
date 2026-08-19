using APIPlatform.Data.Execution;
using APIPlatform.Data.Options;
using APIPlatform.Data.Transactions;
using APIPlatform.Database.Migration.Abstractions;

namespace APIPlatform.Database.Migration.Tests.Fakes;

/// <summary>Controllable IMigration test double — records whether/how it was applied and lets a
/// test force a failure.</summary>
internal sealed class FakeMigration : IMigration
{
    public required string MigrationId { get; init; }
    public required int Version { get; init; }
    public string Description { get; init; } = "Fake migration";
    public required DatabaseProvider SupportedProvider { get; init; }

    public bool Applied { get; private set; }
    public bool ReceivedTransaction { get; private set; }
    public Exception? FailWith { get; set; }

    public Task ApplyAsync(IDatabaseExecutor executor, IDatabaseTransaction? transaction, CancellationToken cancellationToken = default)
    {
        if (FailWith is not null) throw FailWith;

        Applied = true;
        ReceivedTransaction = transaction is not null;
        return Task.CompletedTask;
    }
}
