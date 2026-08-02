using APIPlatform.CrudEngine.Caching;
using APIPlatform.CrudEngine.Models;
using APIPlatform.CrudEngine.Sql.Dialects;

namespace APIPlatform.CrudEngine.Pipeline.Stages;

/// <summary>
/// Stage 1 — understands what the request is trying to do. Resolves EntityDefinition (which
/// already carries fields, relationships, validation rules, and permission/UI metadata per
/// SharedSchema) via the cached IEntityMetadataCache, and resolves the active database provider
/// dialect name. No validation, SQL generation, or execution happens here.
/// </summary>
public sealed class MetadataResolutionStage<TEntity> : IPipelineStage<TEntity> where TEntity : class
{
    private readonly IEntityMetadataCache _metadata;
    private readonly ISqlDialectResolver _dialectResolver;

    public MetadataResolutionStage(IEntityMetadataCache metadata, ISqlDialectResolver dialectResolver)
    {
        _metadata = metadata;
        _dialectResolver = dialectResolver;
    }

    public Task ExecuteAsync(CrudContext<TEntity> context)
    {
        context.EntityDefinition = _metadata.GetDefinition(context.EntityName);
        context.DatabaseProviderName = _dialectResolver.Resolve().Name;
        return Task.CompletedTask;
    }
}
