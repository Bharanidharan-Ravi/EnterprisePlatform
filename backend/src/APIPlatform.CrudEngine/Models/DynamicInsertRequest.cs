namespace APIPlatform.CrudEngine.Models;

/// <summary>
/// A fully data-described write: which table, which column/value pairs — the write-side
/// counterpart to <see cref="DynamicQueryRequest"/>. The engine only ever sees this description
/// and a rows-affected count; it never contains or infers any domain concept (e.g. "user",
/// "password") about what is being written. A caller that needs to store a hashed password
/// hashes it before it goes into <see cref="Values"/> — CrudEngine has no notion of hashing, that
/// composition belongs to whichever app-level controller builds this request
/// (see APIPlatform.Authentication's IPasswordHasher for that piece).
/// </summary>
public sealed class DynamicInsertRequest
{
    /// <summary>Table to insert into.</summary>
    public required string TableName { get; init; }

    /// <summary>Column/value pairs for the new row. Must name at least one column.</summary>
    public required IReadOnlyDictionary<string, object?> Values { get; init; }
}
