using APIPlatform.Data.Execution;
using APIPlatform.Data.Options;
using APIPlatform.Data.Transactions;
using APIPlatform.Database.Migration.Abstractions;

namespace APIPlatform.Playground.Rbac;

/// <summary>
/// Phase 1 (field-level masking) schema, as its own migration — same reasoning as
/// <see cref="RbacRowScopeSqlServerMigration"/>: an already-applied migration is never edited in
/// place, so this is Version 3, not a change to Rbac.Schema.v1 or Rbac.RowScope.v1.
///
/// Backs <see cref="SqlServerFieldPermissionRuleStore"/>, replacing APIPlatform.Rbac's default
/// InMemoryFieldPermissionRuleStore. One row = "field X on entity Y requires permission key Z to
/// be visible/writable, and grants exactly this FieldAccess when the caller holds it" — the same
/// {EntityKey, PermissionKey, Access} shape <see cref="RbacRowPermissionRules"/> uses for rows,
/// with FieldKey added.
/// </summary>
public sealed class RbacFieldMaskSqlServerMigration : IMigration
{
    public string MigrationId => "Rbac.FieldMask.v1";

    public int Version => 3;

    public string Description => "Creates the field-masking RBAC table (FieldPermissionRules).";

    public DatabaseProvider SupportedProvider => DatabaseProvider.SqlServer;

    private const string CreateFieldPermissionRulesTableSql = @"
        CREATE TABLE [RbacFieldPermissionRules] (
            [Id]            UNIQUEIDENTIFIER NOT NULL,
            [TenantId]      NVARCHAR(100)     NOT NULL,
            [EntityKey]     NVARCHAR(200)     NOT NULL,
            [FieldKey]      NVARCHAR(200)     NOT NULL,
            [PermissionKey] NVARCHAR(200)     NOT NULL,
            [Access]        NVARCHAR(10)      NOT NULL,
            CONSTRAINT [PK_RbacFieldPermissionRules] PRIMARY KEY ([Id])
        )";

    private const string CreateFieldPermissionRulesIndexSql = @"
        CREATE INDEX [IX_RbacFieldPermissionRules_TenantId_EntityKey]
            ON [RbacFieldPermissionRules] ([TenantId], [EntityKey])";

    public async Task ApplyAsync(IDatabaseExecutor executor, IDatabaseTransaction? transaction, CancellationToken cancellationToken = default)
    {
        await executor.ExecuteAsync(CreateFieldPermissionRulesTableSql, transaction: transaction, cancellationToken: cancellationToken);
        await executor.ExecuteAsync(CreateFieldPermissionRulesIndexSql, transaction: transaction, cancellationToken: cancellationToken);
    }
}
