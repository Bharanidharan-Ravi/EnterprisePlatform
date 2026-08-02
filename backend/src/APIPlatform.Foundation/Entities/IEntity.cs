namespace APIPlatform.Foundation.Entities;

/// <summary>
/// Marker interface identifying a type as a domain entity. Deliberately carries no
/// members — key shape (Guid, int, string, composite) is intentionally left to each
/// entity so Foundation never forces a key implementation across SQL, SAP, or future stores.
/// </summary>
public interface IEntity
{
}
