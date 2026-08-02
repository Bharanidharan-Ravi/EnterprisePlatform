namespace APIPlatform.CrudEngine.Models;

/// <summary>The set of generic operations CrudEngine can perform against an entity.</summary>
public enum CrudOperationType
{
    GetByKey,
    List,
    Create,
    Update,
    Delete
}
