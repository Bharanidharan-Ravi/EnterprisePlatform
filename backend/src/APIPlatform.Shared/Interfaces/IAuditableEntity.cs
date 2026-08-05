using System;

namespace APIPlatform.Shared.Interfaces;

/// <summary>
/// Interface for entities that require audit tracking.
/// </summary>
public interface IAuditableEntity
{
    /// <summary>
    /// The user identifier who created the entity.
    /// </summary>
    string? CreatedBy { get; set; }

    /// <summary>
    /// The UTC timestamp when the entity was created.
    /// </summary>
    DateTime CreatedUtc { get; set; }

    /// <summary>
    /// The user identifier who last modified the entity.
    /// </summary>
    string? LastModifiedBy { get; set; }

    /// <summary>
    /// The UTC timestamp when the entity was last modified.
    /// </summary>
    DateTime? LastModifiedUtc { get; set; }
}
