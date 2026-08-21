using APIPlatform.Foundation.Entities;

namespace APIPlatform.Playground.Models;

/// <summary>
/// Phase 2 test entity — proves the platform can support one real generic business entity
/// end-to-end (SharedSchema -&gt; CrudEngine -&gt; Database -&gt; API -&gt; RBAC -&gt; UIPlatform Forms)
/// without any business-specific code inside the platform itself. This is application/test-host
/// code; it must never move into APIPlatform.Foundation/CrudEngine/Database/Rbac/Nucleus.SharedSchema.
/// Property names must match <see cref="Metadata.EmployeeEntityDefinitionProvider"/>'s
/// FieldDefinition.Name values case-insensitively — GenericRepository binds by reflection.
/// </summary>
public sealed class Employee : IEntity
{
    public Guid Id { get; set; }
    public required string EmployeeCode { get; set; }
    public required string Name { get; set; }
    public required string Email { get; set; }
    public string? Department { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedOn { get; set; }
    public DateTimeOffset? ModifiedOn { get; set; }
}
