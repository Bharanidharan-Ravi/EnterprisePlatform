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

        // Durable IRoleStore, registered BEFORE AddRbac() so its TryAddSingleton<IRoleStore,
        // InMemoryRoleStore>() is skipped in favor of this one (Rbac's documented "app
        // registrations always win" convention). Replaces the default in-memory store, which is
        // wiped on every restart — see SqlServerRoleStore's doc comment for why it's Singleton.
        services.AddSingleton<IRoleStore, SqlServerRoleStore>();
        services.AddRbac();

        // Employee table migration — lives here (Playground), not inside
        // APIPlatform.Database.Migration, which is a platform assembly.
        services.AddScoped<IMigration, EmployeeSqlServerMigration>();

        // RBAC's durable schema (Roles/UserRoles/PermissionGrants/PolicyRules) — same reasoning
        // as the Employee migration above.
        services.AddScoped<IMigration, RbacSqlServerMigration>();

        services.AddHostedService<Services.EmployeeModuleInitializationService>();

        return services;
    }
}
