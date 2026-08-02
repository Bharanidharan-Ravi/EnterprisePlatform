namespace APIPlatform.Data.Resilience;

/// <summary>
/// Extension point for future transient-failure retry policies (SQL Server transient errors,
/// Azure SQL throttling, etc.). Not implemented in V1 — DatabaseOptions.RetryCount/RetryDelay
/// exist as configuration surface for whichever policy a consumer registers later. The default
/// registration is a no-op so behavior is unchanged until a real policy replaces it.
/// </summary>
public interface IDatabaseRetryPolicy
{
    Task<T> ExecuteAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken = default);
}
