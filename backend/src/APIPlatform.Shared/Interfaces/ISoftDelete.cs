using System;

namespace APIPlatform.Shared.Interfaces;

/// <summary>
/// Interface for entities that support soft deletion.
/// </summary>
public interface ISoftDelete
{
    /// <summary>
    /// Indicates whether the entity is marked as deleted.
    /// </summary>
    bool IsDeleted { get; set; }

    /// <summary>
    /// The UTC timestamp when the entity was soft deleted.
    /// </summary>
    DateTime? DeletedUtc { get; set; }
}
