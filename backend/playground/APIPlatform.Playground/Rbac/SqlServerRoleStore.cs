using APIPlatform.Data.Execution;
using APIPlatform.Rbac.Contracts;
using APIPlatform.Rbac.Models;
using Microsoft.Extensions.DependencyInjection;

namespace APIPlatform.Playground.Rbac;

/// <summary>
/// Durable, SQL Server-backed IRoleStore — replaces APIPlatform.Rbac's default InMemoryRoleStore,
/// whose own doc comment flags it "not durable, not distributed-safe, process-lifetime only."
/// Registered as IRoleStore (Singleton, matching AddRbac()'s own TryAddSingleton default so
/// PermissionResolver's Singleton lifetime needs no change) BEFORE AddRbac() in AddEmployeeModule,
/// per Rbac's documented "app registrations always win" override convention (ServiceCollectionExtensions
/// uses TryAdd* throughout).
///
/// Holds no IDatabaseExecutor directly: IDatabaseExecutor is Scoped (one DB connection per
/// operation), so a Singleton constructor-injecting it would be a captive-dependency bug. Instead
/// opens a short-lived DI scope per call via IServiceScopeFactory — the standard pattern for a
/// singleton that needs a scoped dependency.
///
/// Every write is idempotent (IF NOT EXISTS ... INSERT) since RBAC seeding
/// (EmployeeModuleInitializationService) re-runs on every app start and must not accumulate
/// duplicate rows on a durable store the way it harmlessly did against the in-memory one.
/// </summary>
public sealed class SqlServerRoleStore : IRoleStore, IRoleDefinitionSeeder
{
    // A misconfigured ParentRoleId cycle must not hang role-hierarchy expansion.
    private const int MaxHierarchyHops = 50;

    private readonly IServiceScopeFactory _scopeFactory;

    public SqlServerRoleStore(IServiceScopeFactory scopeFactory) => _scopeFactory = scopeFactory;

    private async Task<T> WithExecutorAsync<T>(Func<IDatabaseExecutor, Task<T>> action)
    {
        using var scope = _scopeFactory.CreateScope();
        var executor = scope.ServiceProvider.GetRequiredService<IDatabaseExecutor>();
        return await action(executor);
    }

    public Task EnsureRoleAsync(Role role, CancellationToken cancellationToken = default) =>
        WithExecutorAsync(executor => executor.ExecuteAsync(
            @"IF NOT EXISTS (SELECT 1 FROM [RbacRoles] WHERE [TenantId] = @TenantId AND [Id] = @Id)
              INSERT INTO [RbacRoles] ([TenantId], [Id], [Name], [ParentRoleId], [IsSystemRole])
              VALUES (@TenantId, @Id, @Name, @ParentRoleId, @IsSystemRole)",
            new Dictionary<string, object?>
            {
                ["TenantId"] = role.TenantId,
                ["Id"] = role.Id,
                ["Name"] = role.Name,
                ["ParentRoleId"] = role.ParentRoleId,
                ["IsSystemRole"] = role.IsSystemRole
            },
            cancellationToken: cancellationToken));

    public Task<IReadOnlyCollection<Role>> GetEffectiveRolesForUserAsync(string tenantId, string userId, CancellationToken cancellationToken = default) =>
        WithExecutorAsync(async executor =>
        {
            var directRoleIds = await executor.QueryAsync<string>(
                "SELECT [RoleId] FROM [RbacUserRoles] WHERE [TenantId] = @TenantId AND [UserId] = @UserId",
                new Dictionary<string, object?> { ["TenantId"] = tenantId, ["UserId"] = userId },
                cancellationToken: cancellationToken);

            var resolved = new Dictionary<string, Role>(StringComparer.OrdinalIgnoreCase);
            var visited = new HashSet<string>(directRoleIds, StringComparer.OrdinalIgnoreCase);
            var frontier = new Queue<string>(directRoleIds);
            var hops = 0;

            while (frontier.Count > 0 && hops++ < MaxHierarchyHops)
            {
                var roleId = frontier.Dequeue();
                var row = await executor.QueryFirstOrDefaultAsync<RoleRow>(
                    "SELECT [Id], [Name], [TenantId], [ParentRoleId], [IsSystemRole] FROM [RbacRoles] WHERE [TenantId] = @TenantId AND [Id] = @Id",
                    new Dictionary<string, object?> { ["TenantId"] = tenantId, ["Id"] = roleId },
                    cancellationToken: cancellationToken);

                if (row is null) continue; // assigned to a role that no longer exists — skip, same as InMemoryRoleStore
                resolved[row.Id] = row.ToModel();

                if (!string.IsNullOrEmpty(row.ParentRoleId) && visited.Add(row.ParentRoleId))
                    frontier.Enqueue(row.ParentRoleId);
            }

            return (IReadOnlyCollection<Role>)resolved.Values.ToList();
        });

    public Task<IReadOnlyCollection<PermissionGrant>> GetGrantsForRolesAsync(string tenantId, IEnumerable<string> roleIds, CancellationToken cancellationToken = default) =>
        WithExecutorAsync(async executor =>
        {
            var ids = roleIds.ToList();
            if (ids.Count == 0) return (IReadOnlyCollection<PermissionGrant>)Array.Empty<PermissionGrant>();

            var rows = await executor.QueryAsync<GrantRow>(
                "SELECT [TenantId], [RoleId], [UserId], [PermissionKey], [Effect] FROM [RbacPermissionGrants] WHERE [TenantId] = @TenantId AND [RoleId] IN @RoleIds",
                new Dictionary<string, object?> { ["TenantId"] = tenantId, ["RoleIds"] = ids },
                cancellationToken: cancellationToken);

            return (IReadOnlyCollection<PermissionGrant>)rows.Select(r => r.ToModel()).ToList();
        });

    public Task<IReadOnlyCollection<PermissionGrant>> GetGrantsForUserAsync(string tenantId, string userId, CancellationToken cancellationToken = default) =>
        WithExecutorAsync(async executor =>
        {
            var rows = await executor.QueryAsync<GrantRow>(
                "SELECT [TenantId], [RoleId], [UserId], [PermissionKey], [Effect] FROM [RbacPermissionGrants] WHERE [TenantId] = @TenantId AND [UserId] = @UserId",
                new Dictionary<string, object?> { ["TenantId"] = tenantId, ["UserId"] = userId },
                cancellationToken: cancellationToken);

            return (IReadOnlyCollection<PermissionGrant>)rows.Select(r => r.ToModel()).ToList();
        });

    public Task<IReadOnlyCollection<PolicyRule>> GetPolicyRulesAsync(string tenantId, CancellationToken cancellationToken = default) =>
        WithExecutorAsync(async executor =>
        {
            var rows = await executor.QueryAsync<PolicyRuleRow>(
                "SELECT [Name], [PermissionKey], [ResourceType] AS [ResourceTypeRaw], [Priority] FROM [RbacPolicyRules] WHERE [TenantId] = @TenantId",
                new Dictionary<string, object?> { ["TenantId"] = tenantId },
                cancellationToken: cancellationToken);

            return (IReadOnlyCollection<PolicyRule>)rows.Select(r => r.ToModel()).ToList();
        });

    public Task AssignRoleAsync(string tenantId, string userId, string roleId, CancellationToken cancellationToken = default) =>
        WithExecutorAsync(executor => executor.ExecuteAsync(
            @"IF NOT EXISTS (SELECT 1 FROM [RbacUserRoles] WHERE [TenantId] = @TenantId AND [UserId] = @UserId AND [RoleId] = @RoleId)
              INSERT INTO [RbacUserRoles] ([TenantId], [UserId], [RoleId]) VALUES (@TenantId, @UserId, @RoleId)",
            new Dictionary<string, object?> { ["TenantId"] = tenantId, ["UserId"] = userId, ["RoleId"] = roleId },
            cancellationToken: cancellationToken));

    public Task GrantPermissionAsync(PermissionGrant grant, CancellationToken cancellationToken = default) =>
        WithExecutorAsync(executor => executor.ExecuteAsync(
            @"IF NOT EXISTS (
                  SELECT 1 FROM [RbacPermissionGrants]
                  WHERE [TenantId] = @TenantId
                    AND ISNULL([RoleId], '') = ISNULL(@RoleId, '')
                    AND ISNULL([UserId], '') = ISNULL(@UserId, '')
                    AND [PermissionKey] = @PermissionKey
                    AND [Effect] = @Effect)
              INSERT INTO [RbacPermissionGrants] ([Id], [TenantId], [RoleId], [UserId], [PermissionKey], [Effect])
              VALUES (@Id, @TenantId, @RoleId, @UserId, @PermissionKey, @Effect)",
            new Dictionary<string, object?>
            {
                ["Id"] = Guid.NewGuid(),
                ["TenantId"] = grant.TenantId,
                ["RoleId"] = grant.RoleId,
                ["UserId"] = grant.UserId,
                ["PermissionKey"] = grant.PermissionKey,
                ["Effect"] = grant.Effect.ToString()
            },
            cancellationToken: cancellationToken));

    public Task RegisterPolicyRuleAsync(string tenantId, PolicyRule rule, CancellationToken cancellationToken = default) =>
        WithExecutorAsync(executor => executor.ExecuteAsync(
            @"IF NOT EXISTS (SELECT 1 FROM [RbacPolicyRules] WHERE [TenantId] = @TenantId AND [Name] = @Name)
              INSERT INTO [RbacPolicyRules] ([Id], [TenantId], [Name], [PermissionKey], [ResourceType], [Priority])
              VALUES (@Id, @TenantId, @Name, @PermissionKey, @ResourceType, @Priority)",
            new Dictionary<string, object?>
            {
                ["Id"] = Guid.NewGuid(),
                ["TenantId"] = tenantId,
                ["Name"] = rule.Name,
                ["PermissionKey"] = rule.PermissionKey,
                ["ResourceType"] = rule.ResourceType.ToString(),
                ["Priority"] = rule.Priority
            },
            cancellationToken: cancellationToken));

    // Mutable row shapes for Dapper materialization — kept separate from the immutable
    // APIPlatform.Rbac.Models types, which use `required` init-only properties.
    private sealed class RoleRow
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string TenantId { get; set; } = string.Empty;
        public string? ParentRoleId { get; set; }
        public bool IsSystemRole { get; set; }

        public Role ToModel() => new()
        {
            Id = Id,
            Name = Name,
            TenantId = TenantId,
            ParentRoleId = ParentRoleId,
            IsSystemRole = IsSystemRole
        };
    }

    private sealed class GrantRow
    {
        public string TenantId { get; set; } = string.Empty;
        public string? RoleId { get; set; }
        public string? UserId { get; set; }
        public string PermissionKey { get; set; } = string.Empty;
        public string Effect { get; set; } = string.Empty;

        public PermissionGrant ToModel() => new()
        {
            TenantId = TenantId,
            RoleId = RoleId,
            UserId = UserId,
            PermissionKey = PermissionKey,
            Effect = Enum.Parse<PermissionEffect>(Effect)
        };
    }

    private sealed class PolicyRuleRow
    {
        public string Name { get; set; } = string.Empty;
        public string PermissionKey { get; set; } = string.Empty;
        public string ResourceTypeRaw { get; set; } = string.Empty;
        public int Priority { get; set; }

        public PolicyRule ToModel() => new()
        {
            Name = Name,
            PermissionKey = PermissionKey,
            ResourceType = Enum.Parse<ResourceType>(ResourceTypeRaw),
            Priority = Priority
        };
    }
}
