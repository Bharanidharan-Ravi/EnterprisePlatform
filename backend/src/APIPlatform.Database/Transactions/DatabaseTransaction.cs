using System.Data;
using APIPlatform.Data.Exceptions;
using APIPlatform.Foundation.Exceptions;

namespace APIPlatform.Data.Transactions;

/// <summary>
/// Default IDatabaseTransaction. Owns both the underlying connection and ADO.NET transaction
/// for its lifetime, disposing both on completion. Internal-only accessors expose the raw
/// connection/transaction so SqlDatabaseExecutor can run commands against the same transaction —
/// these are never part of the public IDatabaseTransaction contract.
/// </summary>
internal sealed class DatabaseTransaction : IDatabaseTransaction
{
    private bool _completed;

    internal DatabaseTransaction(IDbConnection connection, IDbTransaction transaction)
    {
        Connection = connection;
        Transaction = transaction;
    }

    internal IDbConnection Connection { get; }
    internal IDbTransaction Transaction { get; }

    public Task CommitAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            Transaction.Commit();
            _completed = true;
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            throw new DatabaseException("Failed to commit transaction.", ex, ErrorCategory.Infrastructure);
        }
    }

    public Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            Transaction.Rollback();
            _completed = true;
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            throw new DatabaseException("Failed to roll back transaction.", ex, ErrorCategory.Infrastructure);
        }
    }

    public ValueTask DisposeAsync()
    {
        if (!_completed)
        {
            try { Transaction.Rollback(); } catch { /* connection may already be closed; safe to ignore on dispose path */ }
        }
        Transaction.Dispose();
        Connection.Dispose();
        return ValueTask.CompletedTask;
    }
}
