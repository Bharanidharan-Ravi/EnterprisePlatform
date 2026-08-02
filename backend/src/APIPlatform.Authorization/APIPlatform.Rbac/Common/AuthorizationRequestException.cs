namespace APIPlatform.Rbac.Common;

/// <summary>
/// Thrown by the Validation Stage for a structurally malformed AuthorizationRequest (e.g.
/// missing FieldKey on a Field-type request). Distinct from an authorization DENIAL, which is
/// a normal AuthorizationResult, not an exception — a malformed request is a caller bug, a
/// denial is an expected outcome.
/// </summary>
public sealed class AuthorizationRequestException : Exception
{
    public AuthorizationRequestException(string message) : base(message) { }
}
