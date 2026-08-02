namespace APIPlatform.Foundation.Exceptions;

/// <summary>Thrown when the current caller lacks permission to perform the requested operation.</summary>
public sealed class PermissionDeniedException : PlatformException
{
    public PermissionDeniedException(string message) : base(message) { Category = ErrorCategory.PermissionDenied; }
}
