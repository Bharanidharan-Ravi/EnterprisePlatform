namespace APIPlatform.Rbac.Contracts;

/// <summary>
/// The per-user scope VALUES a row-level (or, from Phase 3, policy) delegate keys off — "which
/// department/branch/company is this user in". <see cref="Contexts.AuthorizationContext"/>.Claims
/// carries these as opaque strings and never asks where they came from (Rbac never contains a
/// domain predicate, same reasoning as <see cref="Models.RowPermissionRule"/>) — but SOURCING them
/// live, per request, is itself a generic concern every row/policy-scoped consuming app needs, not
/// an Employee-specific one, so the contract belongs here rather than being reinvented per app.
///
/// <see cref="Services.DefaultAuthorizationContextFactory"/> consumes this directly and merges the
/// result into <c>AuthorizationContext.Claims</c> — an app gets scope-aware Claims for free the
/// moment it registers a real <see cref="IUserScopeStore"/>, with no factory override needed.
///
/// <para><b>Deliberately not sourced from the JWT.</b> A token's scope claim (if any) is a
/// login-time snapshot; reading it at request time would mean moving a user to a new department
/// doesn't take effect until they get a new token, silently deciding which rows they can see off
/// stale data in the meantime. This store is read fresh on every request instead — the token may
/// still carry the same value (for the UI's benefit, via whatever populates
/// <c>UserInfo.DepartmentId</c>/etc.), but enforcement never trusts it.</para>
/// </summary>
public interface IUserScopeStore
{
    /// <summary>All scope values held by one user, keyed by <see cref="Models.ScopeKeys"/>. Empty
    /// when the user has none — callers must treat "absent" as "no scope value", never as
    /// "unrestricted".</summary>
    Task<IReadOnlyDictionary<string, string>> GetScopesAsync(string tenantId, string userId, CancellationToken cancellationToken = default);

    /// <summary>Idempotent upsert of one scope value.</summary>
    Task SetScopeAsync(string tenantId, string userId, string scopeKey, string scopeValue, CancellationToken cancellationToken = default);
}
