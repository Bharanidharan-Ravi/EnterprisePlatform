using System.Data;
using APIPlatform.Data.Execution;
using APIPlatform.Data.Transactions;

namespace APIPlatform.Notification.Tests.Fakes;

internal sealed record RecordedCommand(string Sql, IReadOnlyDictionary<string, object?>? Parameters, IDatabaseTransaction? Transaction);

/// <summary>
/// Hand-written IDatabaseExecutor test double (no mocking library is used anywhere in this
/// codebase). Every call is recorded; behavior for each method is supplied per-test via the
/// Func hooks so NotificationRepository's control flow (which SQL/params it issues, how it
/// reacts to a thrown DatabaseException, whether it commits/rolls back) can be asserted without
/// a live SQL Server or HANA instance — mirroring how APIPlatform.Database.Tests itself avoids
/// requiring a live database.
/// </summary>
internal sealed class FakeDatabaseExecutor : IDatabaseExecutor
{
    public List<RecordedCommand> ExecuteCalls { get; } = [];
    public List<RecordedCommand> QueryCalls { get; } = [];

    public Func<RecordedCommand, int> OnExecute { get; set; } = _ => 0;
    public Func<RecordedCommand, object?> OnQuerySingleOrDefault { get; set; } = _ => null;
    public Func<RecordedCommand, object?> OnExecuteScalar { get; set; } = _ => null;
    public Func<RecordedCommand, IEnumerable<object>> OnQuery { get; set; } = _ => [];
    public Func<IDatabaseTransaction> OnBeginTransaction { get; set; } = () => new FakeDatabaseTransaction();

    public Task<int> ExecuteAsync(string sql, IReadOnlyDictionary<string, object?>? parameters = null, CommandType commandType = CommandType.Text,
        IDatabaseTransaction? transaction = null, int? commandTimeoutSeconds = null, CancellationToken cancellationToken = default)
    {
        var call = new RecordedCommand(sql, parameters, transaction);
        ExecuteCalls.Add(call);
        return Task.FromResult(OnExecute(call));
    }

    public Task<IReadOnlyList<T>> QueryAsync<T>(string sql, IReadOnlyDictionary<string, object?>? parameters = null, CommandType commandType = CommandType.Text,
        IDatabaseTransaction? transaction = null, int? commandTimeoutSeconds = null, CancellationToken cancellationToken = default)
    {
        var call = new RecordedCommand(sql, parameters, transaction);
        QueryCalls.Add(call);
        var result = OnQuery(call).Cast<T>().ToList();
        return Task.FromResult<IReadOnlyList<T>>(result);
    }

    public Task<T?> QuerySingleOrDefaultAsync<T>(string sql, IReadOnlyDictionary<string, object?>? parameters = null, CommandType commandType = CommandType.Text,
        IDatabaseTransaction? transaction = null, int? commandTimeoutSeconds = null, CancellationToken cancellationToken = default)
    {
        var call = new RecordedCommand(sql, parameters, transaction);
        QueryCalls.Add(call);
        return Task.FromResult((T?)OnQuerySingleOrDefault(call));
    }

    public Task<T?> QueryFirstOrDefaultAsync<T>(string sql, IReadOnlyDictionary<string, object?>? parameters = null, CommandType commandType = CommandType.Text,
        IDatabaseTransaction? transaction = null, int? commandTimeoutSeconds = null, CancellationToken cancellationToken = default) =>
        QuerySingleOrDefaultAsync<T>(sql, parameters, commandType, transaction, commandTimeoutSeconds, cancellationToken);

    public Task<T?> ExecuteScalarAsync<T>(string sql, IReadOnlyDictionary<string, object?>? parameters = null, CommandType commandType = CommandType.Text,
        IDatabaseTransaction? transaction = null, int? commandTimeoutSeconds = null, CancellationToken cancellationToken = default)
    {
        var call = new RecordedCommand(sql, parameters, transaction);
        ExecuteCalls.Add(call);
        return Task.FromResult((T?)OnExecuteScalar(call));
    }

    public Task<IMultiResultReader> QueryMultipleAsync(string sql, IReadOnlyDictionary<string, object?>? parameters = null, CommandType commandType = CommandType.Text,
        IDatabaseTransaction? transaction = null, int? commandTimeoutSeconds = null, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("NotificationRepository never calls QueryMultipleAsync.");

    public Task<IDatabaseTransaction> BeginTransactionAsync(IsolationLevel? isolationLevel = null, CancellationToken cancellationToken = default) =>
        Task.FromResult(OnBeginTransaction());
}
