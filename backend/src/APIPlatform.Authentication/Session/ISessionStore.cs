using APIPlatform.Authentication.Interfaces;

namespace APIPlatform.Authentication.Session;

/// <summary>Persistence abstraction for session records. Register a DB/Redis implementation
/// to replace InMemorySessionStore in production.</summary>
public interface ISessionStore
{
    Task SaveAsync(SessionInfo session, CancellationToken cancellationToken = default);
    Task<SessionInfo?> FindAsync(string sessionId, CancellationToken cancellationToken = default);
    Task DeleteAsync(string sessionId, CancellationToken cancellationToken = default);
    Task DeleteAllForUserAsync(string userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SessionInfo>> GetAllForUserAsync(string userId, CancellationToken cancellationToken = default);
}
