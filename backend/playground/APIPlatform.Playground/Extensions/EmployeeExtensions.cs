using APIPlatform.CrudEngine.DependencyInjection;
using APIPlatform.CrudEngine.Defaults;
using APIPlatform.CrudEngine.Interfaces;
using APIPlatform.Foundation.Interfaces;
using APIPlatform.Playground.Defaults;
using APIPlatform.Playground.Infrastructure;
using APIPlatform.Playground.Metadata;
using APIPlatform.Playground.Migrations;
using APIPlatform.Playground.Rbac;
using APIPlatform.Rbac.Contracts;
using APIPlatform.Rbac.DependencyInjection;
using APIPlatform.Database.Migration.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace APIPlatform.Playground.Extensions;

/// <summary>
/// Phase 2 wiring: registers everything needed to prove one generic entity (Employee)
/// end-to-end through CrudEngine + Rbac. This is the only place Employee-specific
/// registrations happen — CrudEngine/Rbac themselves receive no Employee knowledge.
/// </summary>
public static class EmployeeExtensions
{
    public static IServiceCollection AddEmployeeModule(this IServiceCollection services)
    {
        // Bridges Authentication's ICurrentUserContext to Foundation's ICurrentUser/ITenantContext
        // (Req: nothing else in the platform implements these). Must be registered before
        // AddCrudEngine()/AddRbac() resolve their constructor dependencies at runtime.
        services.AddScoped<HttpCurrentUserContextAdapter>();
        services.AddScoped<ICurrentUser>(sp => sp.GetRequiredService<HttpCurrentUserContextAdapter>());
        services.AddScoped<ITenantContext>(sp => sp.GetRequiredService<HttpCurrentUserContextAdapter>());

        // App-supplied metadata/defaults — registered before AddCrudEngine() so they win over
        // the platform's NoOp fallbacks (TryAddSingleton).
        services.AddSingleton<IEntityDefinitionProvider, EmployeeEntityDefinitionProvider>();
        services.AddSingleton<IEntityDefaultValueProvider, EmployeeDefaultValueProvider>();

        services.AddCrudEngine();

        // Durable RBAC stores, all registered BEFORE AddRbac() so its TryAdd* defaults (which are
        // in-memory and wiped on every restart) are skipped in favor of these — Rbac's documented
        // "app registrations always win" convention. See each store's doc comment for why they are
        // Singletons that open their own DI scope rather than injecting the Scoped IDatabaseExecutor.
        services.AddSingleton<IRoleStore, SqlServerRoleStore>();
        services.AddSingleton<IRowPermissionRuleStore, SqlServerRowPermissionRuleStore>();
        services.AddSingleton<IFieldPermissionRuleStore, SqlServerFieldPermissionRuleStore>();

        // Row-scope VALUES per user (which department/branch/company they belong to). IUserScopeStore
        // is an APIPlatform.Rbac contract — Rbac's own DefaultAuthorizationContextFactory already
        // reads it and merges the result into AuthorizationContext.Claims, so no factory override is
        // needed here; consumed both at request time (via that factory) and at login
        // (RbacEnrichedIdentityResolver, so the JWT carries department_id for the UI).
        services.AddSingleton<IUserScopeStore, SqlServerUserScopeStore>();

        services.AddRbac();

        // Row/data-level scoping for every entity, applied through the platform's own CRUD
        // extension point rather than per-controller code. Registered after AddCrudEngine(), which
        // is where ICrudPipelineHook is consumed from (IEnumerable, all hooks run). Bridges
        // CrudEngine and Rbac — two packages deliberately unaware of each other — so it lives here,
        // not inside either package (same reasoning as EmployeesController calling
        // ICrudAuthorizationService directly).
        services.AddCrudPipelineHook<RowScopeCrudHook>();

        // Field-level masking for every entity, same reasoning/placement as RowScopeCrudHook.
        services.AddCrudPipelineHook<FieldMaskCrudHook>();

        // Employee table migration — lives here (Playground), not inside
        // APIPlatform.Database.Migration, which is a platform assembly.
        services.AddScoped<IMigration, EmployeeSqlServerMigration>();

        // RBAC's durable schema (Roles/UserRoles/PermissionGrants/PolicyRules) — same reasoning
        // as the Employee migration above.
        services.AddScoped<IMigration, RbacSqlServerMigration>();

        // Phase 2's additions to that schema (RowPermissionRules/UserScopes), as a separate
        // versioned migration — an already-applied migration is never edited in place.
        services.AddScoped<IMigration, RbacRowScopeSqlServerMigration>();

        // Field-masking's own schema addition (FieldPermissionRules), same reasoning.
        services.AddScoped<IMigration, RbacFieldMaskSqlServerMigration>();

        services.AddHostedService<Services.EmployeeModuleInitializationService>();

        return services;
    }
}
