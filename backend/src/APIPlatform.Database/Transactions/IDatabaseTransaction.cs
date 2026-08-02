namespace APIPlatform.Data.Transactions;

/// <summary>
/// A database transaction spanning one or more IDatabaseExecutor calls. If neither
/// CommitAsync nor RollbackAsync is called before disposal, the transaction is rolled back
/// automatically — callers are never left with an ambiguous half-committed state.
/// </summary>
public interface IDatabaseTransaction : IAsyncDisposable
{
    Task CommitAsync(CancellationToken cancellationToken = default);
    Task RollbackAsync(CancellationToken cancellationToken = default);
}
