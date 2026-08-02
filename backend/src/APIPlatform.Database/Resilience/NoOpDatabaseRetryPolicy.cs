namespace APIPlatform.Data.Resilience;

/// <summary>Default IDatabaseRetryPolicy — executes the operation once, with no retry behavior.</summary>
public sealed class NoOpDatabaseRetryPolicy : IDatabaseRetryPolicy
{
    public Task<T> ExecuteAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken = default) => operation();
}
