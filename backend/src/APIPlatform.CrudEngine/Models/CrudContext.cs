using APIPlatform.Data.Transactions;
using APIPlatform.Foundation.Interfaces;
using APIPlatform.Foundation.Results;
using Nucleus.SharedSchema.Models;

namespace APIPlatform.CrudEngine.Models;

/// <summary>
/// Shared pipeline execution context (Req 3) — replaces long parameter lists between pipeline
/// steps. One instance per CRUD call, mutated in place as it flows through the pipeline.
/// </summary>
public sealed class CrudContext<TEntity> where TEntity : class
{
    public required CrudOperationType Operation { get; init; }
    public required string EntityName { get; init; }
    public EntityDefinition? EntityDefinition { get; set; }

    public TEntity? Entity { get; set; }
    public EntityKeyValues? Key { get; set; }

    /// <summary>Raw List() request shape, set by ICrudEngine before the pipeline runs.
    /// ExecutionPlanningStage turns this into the immutable <see cref="Plan"/> — planning owns
    /// plan construction, callers only describe intent.</summary>
    public IReadOnlyDictionary<string, object?>? RequestedFilters { get; init; }
    public IReadOnlyList<SortSpec>? RequestedSorting { get; init; }
    public PagingSpec? RequestedPaging { get; init; }

    public OperationPlan? Plan { get; set; }

    public ValidationResult? ValidationResult { get; set; }
    public object? ExecutionResult { get; set; }
    public CrudResponse<TEntity>? Response { get; set; }

    public ICurrentUser? CurrentUser { get; init; }
    public ITenantContext? TenantContext { get; init; }
    public IDatabaseTransaction? Transaction { get; set; }
    public string? DatabaseProviderName { get; set; }
    public IDictionary<string, object?> Diagnostics { get; } = new Dictionary<string, object?>();

    public CancellationToken CancellationToken { get; init; }

    /// <summary>Short-circuits the remaining pipeline steps (e.g. validation failed).</summary>
    public bool ShortCircuited { get; set; }
    public ErrorInfo? Error { get; set; }
}
