using APIPlatform.Rbac.Contexts;
using APIPlatform.Rbac.Contracts;

namespace APIPlatform.Rbac.Resolution;

/// <summary>
/// PERMISSION RESOLUTION responsibility only: gathers WHAT is granted or denied — direct user
/// grants + role grants (hierarchy already expanded by IRoleStore), producing the static
/// Allowed/Denied key sets with deny-overrides-allow applied. It also collects the tenant's
/// registered PolicyRule *definitions* (name + target permission key + priority) as data, but
/// does NOT invoke any policy logic — resolving which policies exist is Permission Resolution;
/// running a policy's boolean delegate is POLICY EVALUATION and belongs solely to
/// IPolicyEngine (see PolicyEngine.cs), invoked later from ExecutionStage. Keeping "what
/// applies" (here) separate from "did it pass" (PolicyEngine) is what lets the grant check and
/// the policy check be cached, tested, and reasoned about independently.
/// Cache-first — this is the hottest path in the platform (Master Plan Section 9.7 notes RBAC
/// runs on nearly every request).
/// </summary>
public sealed class PermissionResolver : IPermissionResolver
{
    private readonly IRoleStore _roleStore;
    private readonly IPermissionCache _cache;

    public PermissionResolver(IRoleStore roleStore, IPermissionCache cache)
    {
        _roleStore = roleStore;
        _cache = cache;
    }

    public async Task<PermissionSet> ResolveAsync(string tenantId, string userId, CancellationToken cancellationToken = default)
    {
        var cached = await _cache.GetAsync(tenantId, userId, cancellationToken);
        if (cached is not null)
            return cached;

        var roles = await _roleStore.GetEffectiveRolesForUserAsync(tenantId, userId, cancellationToken);
        var roleGrants = await _roleStore.GetGrantsForRolesAsync(tenantId, roles.Select(r => r.Id), cancellationToken);
        var userGrants = await _roleStore.GetGrantsForUserAsync(tenantId, userId, cancellationToken);
        var policyRules = await _roleStore.GetPolicyRulesAsync(tenantId, cancellationToken);

        var allGrants = roleGrants.Concat(userGrants).ToList();

        var allowed = allGrants
            .Where(g => g.Effect == Models.PermissionEffect.Allow)
            .Select(g => g.PermissionKey)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var denied = allGrants
            .Where(g => g.Effect == Models.PermissionEffect.Deny)
            .Select(g => g.PermissionKey)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        // Deny overrides allow.
        allowed.ExceptWith(denied);

        var set = new PermissionSet
        {
            TenantId = tenantId,
            UserId = userId,
            AllowedKeys = allowed,
            DeniedKeys = denied,
            PolicyRules = policyRules
        };

        await _cache.SetAsync(tenantId, userId, set, cancellationToken: cancellationToken);
        return set;
    }
}
