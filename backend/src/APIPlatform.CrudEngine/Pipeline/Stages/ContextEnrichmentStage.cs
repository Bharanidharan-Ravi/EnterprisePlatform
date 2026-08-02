using APIPlatform.CrudEngine.Defaults;
using APIPlatform.CrudEngine.Models;

namespace APIPlatform.CrudEngine.Pipeline.Stages;

/// <summary>
/// Stage 2 — enriches the context before validation. Today this applies metadata-configured
/// default values (CreatedOn/ModifiedOn/Version/etc. via IDefaultValueProcessor); CurrentUser/
/// TenantContext are already attached to CrudContext by ICrudEngine when it builds the context.
/// Named "ContextEnrichmentStage" (broader than "DefaultStage") so locale, permissions,
/// correlation/request id, etc. can be added here later without a new stage.
/// </summary>
public sealed class ContextEnrichmentStage<TEntity> : IPipelineStage<TEntity> where TEntity : class
{
    private readonly IDefaultValueProcessor _defaults;

    public ContextEnrichmentStage(IDefaultValueProcessor defaults) => _defaults = defaults;

    public Task ExecuteAsync(CrudContext<TEntity> context)
    {
        _defaults.Apply(context);
        return Task.CompletedTask;
    }
}
