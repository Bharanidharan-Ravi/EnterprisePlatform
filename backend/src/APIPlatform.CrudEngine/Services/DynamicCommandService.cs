using APIPlatform.CrudEngine.Interfaces;
using APIPlatform.CrudEngine.Models;
using APIPlatform.CrudEngine.Sql;
using APIPlatform.Data.Execution;
using APIPlatform.Foundation.Exceptions;

namespace APIPlatform.CrudEngine.Services;

/// <summary>
/// Default <see cref="IDynamicCommandService"/> — builds a parameterized INSERT from
/// <see cref="DynamicInsertRequest.TableName"/> and <see cref="DynamicInsertRequest.Values"/> and
/// runs it through the existing <see cref="IDatabaseExecutor"/>. Mirrors
/// <see cref="DynamicQueryService"/>'s identifier-validation rules — TableName and every key in
/// Values arrive on the request, so both are checked against <see cref="SqlIdentifierValidator"/>
/// before being placed in SQL text; every *value* still travels as a SQL parameter.
/// </summary>
public sealed class DynamicCommandService : IDynamicCommandService
{
    private readonly IDatabaseExecutor _executor;

    public DynamicCommandService(IDatabaseExecutor executor)
    {
        _executor = executor;
    }

    public Task<int> InsertAsync(DynamicInsertRequest request, CancellationToken cancellationToken = default)
    {
        Validate(request);

        var columns = request.Values.Keys.ToList();
        var columnList = string.Join(", ", columns);
        var parameterList = string.Join(", ", columns.Select(c => $"@{c}"));
        var sql = $"INSERT INTO {request.TableName} ({columnList}) VALUES ({parameterList})";

        return _executor.ExecuteAsync(sql, request.Values, cancellationToken: cancellationToken);
    }

    private static void Validate(DynamicInsertRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (!SqlIdentifierValidator.IsValidQualifiedName(request.TableName))
            errors["TableName"] = new[] { $"'{request.TableName}' is not a valid table name." };

        if (request.Values.Count == 0)
        {
            errors["Values"] = new[] { "At least one column value is required." };
        }
        else
        {
            var invalidColumns = request.Values.Keys.Where(c => !SqlIdentifierValidator.IsValid(c)).ToList();
            if (invalidColumns.Count > 0)
                errors["Values"] = invalidColumns.Select(c => $"'{c}' is not a valid column name.").ToArray();
        }

        if (errors.Count > 0)
            throw new ValidationException(errors);
    }
}
