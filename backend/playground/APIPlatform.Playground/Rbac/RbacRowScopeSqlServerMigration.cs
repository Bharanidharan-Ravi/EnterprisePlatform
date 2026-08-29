using APIPlatform.Data.Execution;
using APIPlatform.Data.Options;
using APIPlatform.Data.Transactions;
using APIPlatform.Database.Migration.Abstractions;

namespace APIPlatform.Playground.Rbac;

/// <summary>
/// Phase 2 (row/data-level scoping) schema, added as a NEW migration rather than an edit to the
/// already-shipped <see cref="RbacSqlServerMigration"/> (Rbac.Schema.v1) — a migration that has
/// run against a database is immutable; changing it would never re-apply, since
/// MigrationRunner keys history off MigrationId. Version 2 so it runs after every v1 migration.
///
/// Two tables, both the durable replacements for an APIPlatform.Rbac in-memory default (same
/// reasoning and same shape as Phase 0's SqlServerRoleStore replacing InMemoryRoleStore):
///
/// <list type="bullet">
/// <item><b>RbacRowPermissionRules</b> — backs <see cref="SqlServerRowPermissionRuleStore"/>,
/// replacing InMemoryRowPermissionRuleStore. One row = "entity X is row-scoped through the
/// named filter delegate Y". The predicate itself is never stored here: Rbac stores only the
/// delegate's NAME (RowPermissionRule.FilterDelegateKey), and the app registers the actual
/// predicate in IRowFilterRegistry — see EmployeeRowFilters.</item>
/// <item><b>RbacUserScopes</b> — the per-user scope VALUES the filter delegates read
/// ("which department is this user in"). Investigated before adding: [Logins] has no
/// department/branch/company column and [Employees] has no column correlating a row back to a
/// Logins user, so there is no existing table this could have been derived from. Deliberately
/// generic (ScopeKey/ScopeValue rather than three fixed columns) so branch/company scoping in a
/// later phase needs no further schema change.</item>
/// </list>
///
/// Same conventions as the other two migrations here: SQL Server only, no IDENTITY/NEWID()/
/// GETDATE() — every Id is supplied by the caller.
/// </summary>
public sealed class RbacRowScopeSqlServerMigration : IMigration
{
    public string MigrationId => "Rbac.RowScope.v1";

    public int Version => 2;

    public string Description => "Creates the row-scoping RBAC tables (RowPermissionRules, UserScopes).";

    public DatabaseProvider SupportedProvider => DatabaseProvider.SqlServer;

    private const string CreateRowPermissionRulesTableSql = @"
        CREATE TABLE [RbacRowPermissionRules] (
            [Id]                UNIQUEIDENTIFIER NOT NULL,
            [TenantId]          NVARCHAR(100)     NOT NULL,
            [EntityKey]         NVARCHAR(200)     NOT NULL,
            [FilterDelegateKey] NVARCHAR(200)     NOT NULL,
            [TenantScoped]      BIT               NOT NULL,
            CONSTRAINT [PK_RbacRowPermissionRules] PRIMARY KEY ([Id])
        )";

    private const string CreateRowPermissionRulesIndexSql = @"
        CREATE INDEX [IX_RbacRowPermissionRules_TenantId_EntityKey]
            ON [RbacRowPermissionRules] ([TenantId], [EntityKey])";

    // PK is (TenantId, UserId, ScopeKey): one value per scope dimension per user, and the lookup
    // SqlServerUserScopeStore does on every row-scoped request is a covered seek on that key.
    private const string CreateUserScopesTableSql = @"
        CREATE TABLE [RbacUserScopes] (
            [TenantId]   NVARCHAR(100) NOT NULL,
            [UserId]     NVARCHAR(100) NOT NULL,
            [ScopeKey]   NVARCHAR(100) NOT NULL,
            [ScopeValue] NVARCHAR(200) NOT NULL,
            CONSTRAINT [PK_RbacUserScopes] PRIMARY KEY ([TenantId], [UserId], [ScopeKey])
        )";

    public async Task ApplyAsync(IDatabaseExecutor executor, IDatabaseTransaction? transaction, CancellationToken cancellationToken = default)
    {
        await executor.ExecuteAsync(CreateRowPermissionRulesTableSql, transaction: transaction, cancellationToken: cancellationToken);
        await executor.ExecuteAsync(CreateRowPermissionRulesIndexSql, transaction: transaction, cancellationToken: cancellationToken);
        await executor.ExecuteAsync(CreateUserScopesTableSql, transaction: transaction, cancellationToken: cancellationToken);
    }
}
