using System.Data;
using APIPlatform.Data.Execution;
using APIPlatform.Data.Transactions;

namespace APIPlatform.Playground.Tests.TestSupport;

/// <summary>
/// Hand-written IDatabaseExecutor test double — same shape as
/// APIPlatform.Notification.Tests/Fakes/FakeDatabaseExecutor.cs (no mocking library is used
/// anywhere in this codebase). Proves CrudEngine -&gt; GenericRepository -&gt; SQL orchestration
/// only; it does NOT prove CrudEngine -&gt; Dapper -&gt; SQL Server actually works (see
/// EmployeeSqlServerIntegrationTests for that, phase2.md 33).
/// </summary>
internal sealed record RecordedCommand(string Sql, IReadOnlyDictionary<string, object?>? Parameters, IDatabaseTransaction? Transaction);

internal sealed class FakeDatabaseExecutor : IDatabaseExecutor
{
    public List<RecordedCommand> ExecuteCalls { get; } = [];
    public List<RecordedCommand> QueryCalls { get; } = [];

    public Func<RecordedCommand, int> OnExecute { get; set; } = _ => 1;
    public Func<RecordedCommand, object?> OnQuerySingleOrDefault { get; set; } = _ => null;
    public Func<RecordedCommand, object?> OnExecuteScalar { get; set; } = _ => null;
    public Func<RecordedCommand, IEnumerable<object>> OnQuery { get; set; } = _ => [];
    public Func<IDatabaseTransaction> OnBeginTransaction { get; set; } = () => throw new NotSupportedException();

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
        throw new NotSupportedException("Employee CRUD tests never call QueryMultipleAsync.");

    public Task<IDatabaseTransaction> BeginTransactionAsync(IsolationLevel? isolationLevel = null, CancellationToken cancellationToken = default) =>
        Task.FromResult(OnBeginTransaction());
}
