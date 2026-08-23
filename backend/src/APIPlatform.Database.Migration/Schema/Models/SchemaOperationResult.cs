namespace APIPlatform.Database.Migration.Schema.Models;

/// <summary>Why a schema operation did not proceed. Callers (an API controller, typically) map
/// these onto transport status codes; the engine itself stays transport-agnostic.</summary>
public enum SchemaOperationStatus
{
    /// <summary>The operation ran and changed the database.</summary>
    Success,

    /// <summary>The request was malformed — bad identifier, unknown type, no columns, duplicate
    /// column names, more than one primary key. Nothing was executed.</summary>
    Invalid,

    /// <summary>Creating a table that already exists, or dropping/altering one that does not.
    /// Nothing was executed.</summary>
    Conflict,

    /// <summary>The request was valid and the table was already in the requested shape — no
    /// columns needed adding. Nothing was executed.</summary>
    NoChange
}

/// <summary>
/// Outcome of one create/update/delete schema operation, including the exact SQL statements that
/// were executed. Returning the SQL is deliberate: this engine generates DDL at runtime from a
/// request body, so the statements it produced are the most useful thing an operator can see when
/// confirming what a call actually did.
/// </summary>
public sealed class SchemaOperationResult
{
    public SchemaOperationStatus Status { get; init; }

    /// <summary>Physical table name the operation targeted.</summary>
    public string Table { get; init; } = string.Empty;

    /// <summary>Human-readable summary of what happened, or why it did not.</summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>Column names created or added by this operation; empty for a drop or a failure.</summary>
    public IReadOnlyList<string> Columns { get; init; } = [];

    /// <summary>The DDL statements executed, in order; empty when nothing ran.</summary>
    public IReadOnlyList<string> ExecutedStatements { get; init; } = [];

    public static SchemaOperationResult Invalid(string table, string message) =>
        new() { Status = SchemaOperationStatus.Invalid, Table = table, Message = message };

    public static SchemaOperationResult Conflict(string table, string message) =>
        new() { Status = SchemaOperationStatus.Conflict, Table = table, Message = message };
}
