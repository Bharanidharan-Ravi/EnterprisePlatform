using APIPlatform.CrudEngine.Adapters;
using APIPlatform.CrudEngine.Caching;
using APIPlatform.CrudEngine.Defaults;
using APIPlatform.CrudEngine.Engine;
using APIPlatform.CrudEngine.Interfaces;
using APIPlatform.CrudEngine.Pipeline;
using APIPlatform.CrudEngine.Registry;
using APIPlatform.CrudEngine.Repositories;
using APIPlatform.CrudEngine.Services;
using APIPlatform.CrudEngine.Sql;
using APIPlatform.CrudEngine.Sql.Builders;
using APIPlatform.CrudEngine.Sql.Dialects;
using APIPlatform.CrudEngine.Validation;
using APIPlatform.Foundation.Entities;
using APIPlatform.Foundation.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace APIPlatform.CrudEngine.DependencyInjection;

/// <summary>Registers APIPlatform.CrudEngine services.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the full CrudEngine pipeline (Req 1-10): metadata caching, default values,
    /// validation, hooks, operation planning/query building, and the public ICrudEngine&lt;T&gt;
    /// facade. IRepository&lt;T&gt; remains registered underneath as an execution detail — resolves
    /// to CompositeRepository when <paramref name="enableProcedureBindings"/> is true, otherwise
    /// plain GenericRepository (unchanged Step 2 behavior). Safe to call with zero extra app
    /// registrations: NoOp defaults/multi-result providers are used until the app supplies its own.
    /// </summary>
    public static IServiceCollection AddCrudEngine(this IServiceCollection services, bool enableProcedureBindings = false)
    {
        // Repository layer — implementation detail behind ICrudEngine<T> (Req 1)
        services.AddScoped(typeof(GenericRepository<>));
        services.AddScoped(typeof(IEntityService<>), typeof(Services.EntityService<>));
        services.AddScoped(typeof(IRepository<>), enableProcedureBindings
            ? typeof(CompositeRepository<>)
            : typeof(GenericRepository<>));

        // Metadata caching (Req 12)
        services.AddSingleton<IEntityMetadataCache, EntityMetadataCache>();

        // Default values + validation (Req 7, Req 8) — safe no-op fallbacks
        services.TryAddSingleton<IEntityDefaultValueProvider, NoOpEntityDefaultValueProvider>();
        services.TryAddSingleton<IMultiResultOperationProvider, NoOpMultiResultOperationProvider>();

        // Stored-procedure execution port (Req 14) — resolves the ASSUMPTION BOUNDARY flagged on
        // IProcedurePort. Backed by APIPlatform.Data's IDatabaseExecutor by default; an app may
        // override with its own adapter by registering IProcedurePort before calling AddCrudEngine().
        services.TryAddScoped<IProcedurePort, DatabaseExecutorProcedurePort>();
        services.AddScoped<IDefaultValueProcessor, DefaultValueProcessor>();
        services.AddScoped<IValidationPipeline, MetadataValidationPipeline>();

        // Query builders (Req 5, Req 6) + provider dialect (Req 11)
        services.AddSingleton<IFilterClauseBuilder, FilterClauseBuilder>();
        services.AddSingleton<ISortClauseBuilder, SortClauseBuilder>();
        services.AddSingleton<IPagingClauseBuilder, PagingClauseBuilder>();
        services.AddSingleton<QuerySqlBuilder>();
        services.AddSingleton<ISqlDialectResolver, DefaultSqlDialectResolver>();

        // Pipeline stages (one responsibility each) + public engine facade (Req 2, Req 3, Req 10)
        services.AddScoped(typeof(Pipeline.Stages.MetadataResolutionStage<>));
        services.AddScoped(typeof(Pipeline.Stages.ContextEnrichmentStage<>));
        services.AddScoped(typeof(Pipeline.Stages.ValidationStage<>));
        services.AddScoped(typeof(Pipeline.Stages.ExecutionPlanningStage<>));
        services.AddScoped(typeof(Pipeline.Stages.ExecutionStage<>));
        services.AddScoped(typeof(Pipeline.Stages.ResponseMappingStage<>));
        services.AddScoped(typeof(ICrudPipeline<>), typeof(Pipeline.CrudPipeline<>));
        services.AddScoped(typeof(ICrudEngine<>), typeof(CrudEngine<>));

        // Batch + multi-result execution (preserved from prior revision — Req 14)
        services.AddSingleton<IEntityTypeRegistry, EntityTypeRegistry>();
        services.AddScoped<IBatchCrudExecutor, BatchCrudExecutor>();
        services.AddScoped<IMultiResultQueryService, MultiResultQueryService>();

        return services;
    }

    /// <summary>Registers an entity's CLR type against its EntityDefinition name — required for
    /// any entity used through IBatchCrudExecutor or a multi-result ListAsync binding. Order-
    /// independent: seeds are collected and applied when IEntityTypeRegistry is first resolved.</summary>
    public static IServiceCollection AddEntityType<TEntity>(this IServiceCollection services, string? entityName = null)
        where TEntity : class, IEntity
    {
        services.AddSingleton<IEntityTypeSeed>(new EntityTypeSeed(entityName ?? typeof(TEntity).Name, typeof(TEntity)));
        return services;
    }

    /// <summary>Registers a Before/After extension hook (Req 9) — Audit, Workflow, Notification,
    /// etc. Multiple hooks may be registered; all run, in registration order.</summary>
    public static IServiceCollection AddCrudPipelineHook<THook>(this IServiceCollection services)
        where THook : class, APIPlatform.CrudEngine.Hooks.ICrudPipelineHook
    {
        services.AddScoped<APIPlatform.CrudEngine.Hooks.ICrudPipelineHook, THook>();
        return services;
    }
}
