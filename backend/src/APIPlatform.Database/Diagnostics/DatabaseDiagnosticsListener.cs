using System.Data;

namespace APIPlatform.Data.Diagnostics;

/// <summary>
/// Extension point for observing execution (timing, failures) without Database depending on
/// any logging package. Deliberately an abstract class with virtual no-op methods rather than
/// an interface — future hooks can be added as new virtual members without breaking existing
/// listeners, which a new interface member would. A future APIPlatform.Logging package
/// registers one or more subclasses via DI; none are registered by default.
/// </summary>
public abstract class DatabaseDiagnosticsListener
{
    public virtual void OnCommandExecuting(string commandText, CommandType commandType) { }
    public virtual void OnCommandExecuted(string commandText, CommandType commandType, TimeSpan duration) { }
    public virtual void OnCommandFailed(string commandText, CommandType commandType, Exception exception, TimeSpan duration) { }
}
