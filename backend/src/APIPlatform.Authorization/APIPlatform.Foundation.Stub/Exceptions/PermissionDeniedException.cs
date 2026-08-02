namespace APIPlatform.Foundation.Exceptions;

/// <summary>
/// STUB — placeholder for the real APIPlatform.Foundation package. Named in Master Plan
/// Section 3.1 (Foundation Exceptions/). Thrown by consumers (e.g. Middleware) when an
/// APIPlatform.Rbac AuthorizationResult.Allowed is false and the caller chooses to fail hard
/// rather than branch on the result. Rbac itself never throws this — it returns a result.
/// </summary>
public sealed class PermissionDeniedException : Exception
{
    public PermissionDeniedException(string message) : base(message) { }
}
