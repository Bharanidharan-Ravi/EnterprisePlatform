using APIPlatform.Rbac.Cache;
using APIPlatform.Rbac.Contracts;
using APIPlatform.Rbac.Hooks;
using APIPlatform.Rbac.Pipeline.Stages;
using APIPlatform.Rbac.Policy;
using APIPlatform.Rbac.Resolution;
using APIPlatform.Rbac.Services;
using APIPlatform.Rbac.Stores;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace APIPlatform.Rbac.DependencyInjection;

/// <summary>
/// Public DI entry point: services.AddRbac(). Registers defaults for every abstraction;
/// consuming apps override any of them by registering their own implementation BEFORE calling
/// AddRbac() (TryAdd* is used throughout so app registrations always win).
/// Lifetime notes: singletons hold no per-request/scoped dependencies. Everything that
/// ultimately depends on IAuthorizationContextFactory (scoped, since it reads per-request
/// identity) is registered Scoped to avoid a captive-dependency bug.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddRbac(this IServiceCollection services, Action<RbacOptions>? configure = null)
    {
        var options = new RbacOptions();
        configure?.Invoke(options);
        services.AddSingleton(options);

        services.AddMemoryCache();

        // --- Singletons: stateless or thread-safe, no scoped dependencies ---
        services.TryAddSingleton<IPermissionCache, MemoryPermissionCache>();
        services.TryAddSingleton<IRoleStore, InMemoryRoleStore>();
        services.TryAddSingleton<IFieldPermissionRuleStore, InMemoryFieldPermissionRuleStore>();
        services.TryAddSingleton<IRowPermissionRuleStore, InMemoryRowPermissionRuleStore>();
        services.TryAddSingleton<IPolicyRuleRegistry, PolicyRuleRegistry>();
        services.TryAddSingleton<IRowFilterRegistry, RowFilterRegistry>();
        services.TryAddSingleton<IPolicyEngine, PolicyEngine>();
        services.TryAddSingleton<IPermissionResolver, PermissionResolver>();

        // --- Scoped: depend (directly or transitively) on per-request identity/context ---
        services.TryAddScoped<IAuthorizationContextFactory, DefaultAuthorizationContextFactory>();

        services.TryAddScoped<PermissionResolutionStage>();
        services.TryAddScoped<ContextEnrichmentStage>();
        services.TryAddScoped<ValidationStage>();
        services.TryAddScoped<PlanningStage>();
        services.TryAddScoped<ExecutionStage>();
        services.TryAddScoped<ResponseMappingStage>();
        services.TryAddScoped<AuthorizationHookInvoker>();

        services.TryAddScoped<IPermissionEvaluator, PermissionEvaluator>();
        services.TryAddScoped<IRoleService, RoleService>();
        services.TryAddScoped<IMenuAuthorizationService, MenuAuthorizationService>();
        services.TryAddScoped<IFieldAuthorizationService, FieldAuthorizationService>();
        services.TryAddScoped<IRowAuthorizationFilterProvider, RowAuthorizationFilterProvider>();
        services.TryAddScoped<IFeatureAuthorizationService, FeatureAuthorizationService>();
        services.TryAddScoped<ICrudAuthorizationService, CrudAuthorizationService>();

        return services;
    }
}
