using APIPlatform.Authentication.Interfaces;
using APIPlatform.Authentication.Models;
using APIPlatform.CrudEngine.Interfaces;
using APIPlatform.CrudEngine.Models;

namespace APIPlatform.Playground.Resolvers;

/// <summary>
/// Real IIdentityResolver, backed by [Nucleus].[dbo].[Logins] (this app's own connection) —
/// replaces PlaygroundIdentityResolver's two hardcoded logins for anything that goes through
/// IAuthenticationService (Login, refresh's re-resolution, etc.).
///
/// <para>Looks the row up through IDynamicQueryService rather than a hand-written repository —
/// same "engine only processes a description" contract every Dynamic* endpoint uses — but
/// TableName/Columns are fixed constants here, not caller-supplied, unlike
/// DynamicDataController/AuthenticationController.Import. Login is a security-sensitive path: the
/// login request itself must never get to name its own source table, or any caller could probe
/// arbitrary tables through the login endpoint. Fixing the table/columns in this one small class is
/// exactly the "config the consuming app supplies" IIdentityResolver's own contract calls for.</para>
/// </summary>
public sealed class LoginsIdentityResolver : IIdentityResolver
{
    private const string TableName = "Logins";

    private static readonly IReadOnlyList<string> Columns = new[]
    {
        "Id", "Username", "FirstName", "LastName", "PasswordHash", "Email",
        "IsActive", "IsLocked", "FailedAttemptCount", "Dbname"
    };

    private readonly IDynamicQueryService _dynamicQuery;

    public LoginsIdentityResolver(IDynamicQueryService dynamicQuery)
    {
        _dynamicQuery = dynamicQuery;
    }

    public async Task<UserInfo?> ResolveAsync(string loginIdentifier, string? tenantId, CancellationToken cancellationToken = default)
    {
        var filters = new Dictionary<string, object?> { ["Username"] = loginIdentifier };
        if (!string.IsNullOrWhiteSpace(tenantId))
            filters["Dbname"] = tenantId;

        return await ResolveByFilterAsync(filters, loginIdentifier, cancellationToken);
    }

    /// <summary>Refresh-token rotation only ever has the Id (never a password/login identifier),
    /// so it looks the same row up by Id instead of Username.</summary>
    public Task<UserInfo?> ResolveByIdAsync(string userId, CancellationToken cancellationToken = default) =>
        ResolveByFilterAsync(new Dictionary<string, object?> { ["Id"] = userId }, userId, cancellationToken);

    private async Task<UserInfo?> ResolveByFilterAsync(
        IReadOnlyDictionary<string, object?> filters, string fallbackUsername, CancellationToken cancellationToken)
    {
        var rows = await _dynamicQuery.QueryAsync(new DynamicQueryRequest
        {
            TableName = TableName,
            Columns = Columns,
            Filters = filters,
            Top = 1
        }, cancellationToken);

        var row = rows.FirstOrDefault();
        if (row is null) return null;

        return new UserInfo
        {
            UserId = row["Id"]?.ToString() ?? string.Empty,
            Username = row["Username"]?.ToString() ?? fallbackUsername,
            Email = row.GetValueOrDefault("Email")?.ToString(),
            PasswordHash = row["PasswordHash"]?.ToString() ?? string.Empty,
            IsActive = ToBool(row.GetValueOrDefault("IsActive")),
            IsLocked = ToBool(row.GetValueOrDefault("IsLocked")),
            FailedAttemptCount = ToInt(row.GetValueOrDefault("FailedAttemptCount")),
            // Dbname is the closest thing this schema has to a tenant discriminator — carried
            // through so ClaimsBuilder still emits a tenant_id claim, same as the old
            // SEC_USER.DbName-driven flow.
            TenantId = row.GetValueOrDefault("Dbname")?.ToString()
        };
    }

    private static bool ToBool(object? value) => value switch
    {
        null => false,
        bool b => b,
        _ => Convert.ToInt64(value) != 0
    };

    private static int ToInt(object? value) => value is null ? 0 : Convert.ToInt32(value);
}
