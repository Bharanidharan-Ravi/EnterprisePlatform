namespace APIPlatform.Foundation.Constants;

/// <summary>Common HTTP header names used across the platform (e.g. by future tenant resolvers).</summary>
public static class WellKnownHeaders
{
    public const string TenantId = "X-Tenant-Id";
    public const string CorrelationId = "X-Correlation-Id";
}
