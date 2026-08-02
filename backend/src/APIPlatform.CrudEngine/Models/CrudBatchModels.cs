namespace APIPlatform.CrudEngine.Models;

/// <summary>One unit of work in a batch request — e.g. "List DepartmentMaster since T" or
/// "GetByKey Ticket #42". ResultKey lets the caller run the same entity+operation more than
/// once under different keys in a single batch.</summary>
public sealed class CrudBatchUnit
{
    public required string ResultKey { get; init; }
    public required string EntityName { get; init; }
    public required CrudOperationType Operation { get; init; }
    public EntityKeyValues? Key { get; init; }
    public object? Payload { get; init; }
    public DateTimeOffset? Since { get; init; }
}

/// <summary>Plain key/value bag for GetByKey/Delete — avoids a hard dependency from Models
/// on Foundation.Interfaces.EntityKey construction details at the call site.</summary>
public sealed class EntityKeyValues : Dictionary<string, object?>
{
    public EntityKeyValues() : base(StringComparer.OrdinalIgnoreCase) { }
}

/// <summary>Outcome of one CrudBatchUnit.</summary>
public sealed class CrudBatchResult
{
    public bool Ok { get; init; }
    public object? Data { get; init; }
    public string? ErrorCode { get; init; }
    public string? ErrorMessage { get; init; }
}

/// <summary>Full batch response — keyed by CrudBatchUnit.ResultKey, mirrors partial-success
/// semantics (some keys ok, some failed) rather than all-or-nothing.</summary>
public sealed class CrudBatchResponse
{
    public Dictionary<string, CrudBatchResult> Results { get; } = new(StringComparer.Ordinal);
    public bool AnySuccess => Results.Values.Any(r => r.Ok);
    public bool AnyFailure => Results.Values.Any(r => !r.Ok);
}
