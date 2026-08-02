using APIPlatform.Foundation.Exceptions;

namespace APIPlatform.Data.Exceptions;

/// <summary>
/// Thrown for connection, command, or transaction failures inside APIPlatform.Data. Wraps the
/// original provider-specific exception (e.g. SqlException) as InnerException rather than
/// letting it escape directly, so consumers depend on one exception type across providers.
/// </summary>
public sealed class DatabaseException : PlatformException
{
    public DatabaseException(string message, ErrorCategory category = ErrorCategory.Infrastructure)
        : base(message) => Category = category;

    public DatabaseException(string message, Exception innerException, ErrorCategory category = ErrorCategory.Infrastructure)
        : base(message, innerException) => Category = category;
}
