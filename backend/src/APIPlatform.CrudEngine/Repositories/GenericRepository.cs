using APIPlatform.CrudEngine.Sql;
using APIPlatform.Data.Execution;
using APIPlatform.Foundation.Entities;
using APIPlatform.Foundation.Interfaces;
using APIPlatform.Foundation.Results;
using Nucleus.SharedSchema.Models;

namespace APIPlatform.CrudEngine.Repositories;

/// <summary>
/// Generic Dapper-backed IRepository&lt;TEntity&gt; implementation. Table name, primary key
/// field(s), and tenant scoping are resolved from EntityDefinition via IEntityDefinitionProvider.
/// No per-entity repository class is hand-written — SQL generation and execution are entirely
/// metadata-driven.
/// </summary>
public sealed class GenericRepository<TEntity> : IRepository<TEntity> where TEntity : class, IEntity
{
    private readonly IDatabaseExecutor _executor;
    private readonly IEntityDefinitionProvider _definitionProvider;
    private readonly ITenantContext _tenantContext;
    private readonly EntityDefinition _definition;
    private readonly IReadOnlyList<string> _keyFieldNames;

    public GenericRepository(
        IDatabaseExecutor executor,
        IEntityDefinitionProvider definitionProvider,
        ITenantContext tenantContext)
    {
        _executor = executor;
        _definitionProvider = definitionProvider;
        _tenantContext = tenantContext;
        _definition = _definitionProvider.GetDefinition(typeof(TEntity).Name);
        _keyFieldNames = _definition.Fields.Where(f => f.IsPrimaryKey).Select(f => f.Name).ToList();

        if (_keyFieldNames.Count == 0)
            throw new InvalidOperationException(
                $"EntityDefinition for '{typeof(TEntity).Name}' has no field marked IsPrimaryKey. GenericRepository requires at least one.");
    }

    public async Task<TEntity?> GetByKeyAsync(EntityKey key, CancellationToken cancellationToken = default)
    {
        var sql = SqlQueryBuilder.SelectByKey(_definition, _keyFieldNames);
        var parameters = BuildParameters(key);
        return await _executor.QuerySingleOrDefaultAsync<TEntity>(sql, parameters, cancellationToken: cancellationToken);
    }

    public async Task<IReadOnlyList<TEntity>> ListAsync(CancellationToken cancellationToken = default)
    {
        var sql = SqlQueryBuilder.SelectAll(_definition);
        var parameters = BuildParameters(null);
        return await _executor.QueryAsync<TEntity>(sql, parameters, cancellationToken: cancellationToken);
    }

    public async Task<Result<TEntity>> AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        var sql = SqlQueryBuilder.Insert(_definition);
        var parameters = ExtractParameters(entity);
        var rows = await _executor.ExecuteAsync(sql, parameters, cancellationToken: cancellationToken);
        return rows > 0
            ? Result<TEntity>.Success(entity)
            : Result<TEntity>.Failure(new ErrorInfo { Code = "insert_failed", Message = $"Insert into {_definition.SourceName} affected 0 rows." });
    }

    public async Task<OperationResult> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        var sql = SqlQueryBuilder.Update(_definition, _keyFieldNames);
        var parameters = ExtractParameters(entity);
        var rows = await _executor.ExecuteAsync(sql, parameters, cancellationToken: cancellationToken);
        return rows > 0
            ? OperationResult.Success()
            : OperationResult.Failure(new ErrorInfo { Code = "update_failed", Message = $"Update on {_definition.SourceName} affected 0 rows." });
    }

    public async Task<OperationResult> DeleteAsync(EntityKey key, CancellationToken cancellationToken = default)
    {
        var sql = SqlQueryBuilder.Delete(_definition, _keyFieldNames);
        var parameters = BuildParameters(key);
        var rows = await _executor.ExecuteAsync(sql, parameters, cancellationToken: cancellationToken);
        return rows > 0
            ? OperationResult.Success()
            : OperationResult.Failure(new ErrorInfo { Code = "delete_failed", Message = $"Delete on {_definition.SourceName} affected 0 rows." });
    }

    private Dictionary<string, object?> BuildParameters(EntityKey? key)
    {
        var parameters = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        if (key is not null)
            foreach (var (k, v) in key) parameters[k] = v;

        if (_definition.IsTenantScoped)
            parameters["TenantId"] = _tenantContext.TenantId;

        return parameters;
    }

    private Dictionary<string, object?> ExtractParameters(TEntity entity)
    {
        var parameters = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        var properties = typeof(TEntity).GetProperties();

        foreach (var prop in properties)
        {
            if (_definition.Fields.Any(f => f.Name.Equals(prop.Name, StringComparison.OrdinalIgnoreCase)))
                parameters[prop.Name] = prop.GetValue(entity);
        }

        if (_definition.IsTenantScoped)
            parameters["TenantId"] = _tenantContext.TenantId;

        return parameters;
    }
}
