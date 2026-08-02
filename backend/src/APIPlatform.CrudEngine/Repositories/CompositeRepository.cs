using APIPlatform.CrudEngine.Adapters;
using APIPlatform.CrudEngine.Interfaces;
using APIPlatform.CrudEngine.Models;
using APIPlatform.Foundation.Entities;
using APIPlatform.Foundation.Interfaces;
using APIPlatform.Foundation.Results;

namespace APIPlatform.CrudEngine.Repositories;

/// <summary>
/// IRepository&lt;TEntity&gt; that runs each operation through generated SQL (GenericRepository)
/// unless an <see cref="EntityOperationBinding"/> says that operation should hit a stored
/// procedure instead. This is what makes CrudEngine "all CRUD, config-driven, scalable" —
/// legacy/complex-query entities can bind to SPs per-operation without a bespoke repository
/// class per entity, and with zero business logic in Nucleus (bindings are pure config).
/// </summary>
public sealed class CompositeRepository<TEntity> : IRepository<TEntity> where TEntity : class, IEntity
{
    private readonly GenericRepository<TEntity> _sqlRepository;
    private readonly IProcedurePort _procedures;
    private readonly IEntityOperationBindingProvider _bindings;
    private readonly IMultiResultOperationProvider _multiResultConfigs;
    private readonly IEntityTypeRegistry _typeRegistry;
    private readonly string _entityName = typeof(TEntity).Name;

    public CompositeRepository(
        GenericRepository<TEntity> sqlRepository,
        IProcedurePort procedures,
        IEntityOperationBindingProvider bindings,
        IMultiResultOperationProvider multiResultConfigs,
        IEntityTypeRegistry typeRegistry)
    {
        _sqlRepository = sqlRepository;
        _procedures = procedures;
        _bindings = bindings;
        _multiResultConfigs = multiResultConfigs;
        _typeRegistry = typeRegistry;
    }

    public async Task<TEntity?> GetByKeyAsync(EntityKey key, CancellationToken cancellationToken = default)
    {
        var binding = _bindings.TryGetBinding(_entityName);
        if (binding?.ProcedureNames.TryGetValue(CrudOperationType.GetByKey, out var proc) == true)
            return await _procedures.QuerySingleOrDefaultAsync<TEntity>(proc, ToParams(key), cancellationToken);

        return await _sqlRepository.GetByKeyAsync(key, cancellationToken);
    }

    public async Task<IReadOnlyList<TEntity>> ListAsync(CancellationToken cancellationToken = default)
    {
        var binding = _bindings.TryGetBinding(_entityName);

        if (binding?.ListIsMultiResult == true && binding.MultiResultOperationKey is not null)
        {
            var config = _multiResultConfigs.TryGetConfig(binding.MultiResultOperationKey)
                ?? throw new InvalidOperationException($"MultiResultOperationKey '{binding.MultiResultOperationKey}' has no registered config.");

            var resultSets = config.Results
                .Select(r => (r.ResultKey, _typeRegistry.Resolve(r.EntityName)))
                .ToList();

            var data = await _procedures.QueryMultipleAsync(config.ProcedureName, EmptyParams, resultSets, cancellationToken);
            // The entity's own result set is the one keyed by its own name.
            return data.TryGetValue(_entityName, out var rows) ? rows.Cast<TEntity>().ToList() : Array.Empty<TEntity>();
        }

        if (binding?.ProcedureNames.TryGetValue(CrudOperationType.List, out var proc) == true)
            return await _procedures.QueryAsync<TEntity>(proc, EmptyParams, cancellationToken);

        return await _sqlRepository.ListAsync(cancellationToken);
    }

    public async Task<Result<TEntity>> AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        var binding = _bindings.TryGetBinding(_entityName);
        if (binding?.ProcedureNames.TryGetValue(CrudOperationType.Create, out var proc) == true)
        {
            var rows = await _procedures.ExecuteAsync(proc, ExtractParams(entity), cancellationToken);
            return rows > 0
                ? Result<TEntity>.Success(entity)
                : Result<TEntity>.Failure(new ErrorInfo { Code = "insert_failed", Message = $"Procedure {proc} affected 0 rows." });
        }

        return await _sqlRepository.AddAsync(entity, cancellationToken);
    }

    public async Task<OperationResult> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        var binding = _bindings.TryGetBinding(_entityName);
        if (binding?.ProcedureNames.TryGetValue(CrudOperationType.Update, out var proc) == true)
        {
            var rows = await _procedures.ExecuteAsync(proc, ExtractParams(entity), cancellationToken);
            return rows > 0
                ? OperationResult.Success()
                : OperationResult.Failure(new ErrorInfo { Code = "update_failed", Message = $"Procedure {proc} affected 0 rows." });
        }

        return await _sqlRepository.UpdateAsync(entity, cancellationToken);
    }

    public async Task<OperationResult> DeleteAsync(EntityKey key, CancellationToken cancellationToken = default)
    {
        var binding = _bindings.TryGetBinding(_entityName);
        if (binding?.ProcedureNames.TryGetValue(CrudOperationType.Delete, out var proc) == true)
        {
            var rows = await _procedures.ExecuteAsync(proc, ToParams(key), cancellationToken);
            return rows > 0
                ? OperationResult.Success()
                : OperationResult.Failure(new ErrorInfo { Code = "delete_failed", Message = $"Procedure {proc} affected 0 rows." });
        }

        return await _sqlRepository.DeleteAsync(key, cancellationToken);
    }

    private static readonly Dictionary<string, object?> EmptyParams = new();

    private static Dictionary<string, object?> ToParams(EntityKey key)
    {
        var dict = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var (k, v) in key) dict[k] = v;
        return dict;
    }

    private static Dictionary<string, object?> ExtractParams(TEntity entity)
    {
        var dict = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var prop in typeof(TEntity).GetProperties())
            dict[prop.Name] = prop.GetValue(entity);
        return dict;
    }
}
