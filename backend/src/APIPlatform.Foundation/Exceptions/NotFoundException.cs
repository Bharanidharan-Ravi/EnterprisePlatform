namespace APIPlatform.Foundation.Exceptions;

/// <summary>Thrown when a requested entity or resource does not exist.</summary>
public sealed class NotFoundException : PlatformException
{
    public NotFoundException(string message) : base(message) { Category = ErrorCategory.NotFound; }

    public NotFoundException(string entityName, object id)
        : base($"{entityName} with id '{id}' was not found.") { Category = ErrorCategory.NotFound; }
}
