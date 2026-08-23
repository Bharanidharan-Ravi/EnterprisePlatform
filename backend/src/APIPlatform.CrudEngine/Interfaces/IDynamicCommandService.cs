using APIPlatform.CrudEngine.Models;

namespace APIPlatform.CrudEngine.Interfaces;

/// <summary>
/// Runs a <see cref="DynamicInsertRequest"/> — the write-side counterpart to
/// <see cref="IDynamicQueryService"/>. Same contract shape: the caller describes a table and
/// column values, the engine never hardcodes either. Table/column identifiers are validated the
/// same way DynamicQueryService validates them; see DynamicCommandService for the rules.
/// </summary>
public interface IDynamicCommandService
{
    /// <summary>Inserts one row and returns the number of rows affected (0 or 1).</summary>
    Task<int> InsertAsync(DynamicInsertRequest request, CancellationToken cancellationToken = default);
}
