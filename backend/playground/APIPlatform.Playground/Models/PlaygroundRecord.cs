using System;

namespace APIPlatform.Playground.Models;

/// <summary>
/// A completely generic model used ONLY for validating the framework's database execution capabilities.
/// It must never be used for business logic.
/// </summary>
public sealed class PlaygroundRecord
{
    /// <summary>
    /// Gets or sets the unique identifier.
    /// </summary>
    public Guid Id { get; set; }
    
    /// <summary>
    /// Gets or sets the name.
    /// </summary>
    public required string Name { get; set; }
    
    /// <summary>
    /// Gets or sets the value.
    /// </summary>
    public string? Value { get; set; }
    
    /// <summary>
    /// Gets or sets the creation timestamp.
    /// </summary>
    public DateTimeOffset CreatedOn { get; set; }
}
