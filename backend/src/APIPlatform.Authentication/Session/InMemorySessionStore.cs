using APIPlatform.Authentication.Interfaces;

namespace APIPlatform.Authentication.Session;

public sealed class InMemorySessionStore : ISessionStore
{
    private readonly Dictionary<string, SessionInfo> _sessions = new(StringComparer.Ordinal);
    private readonly Lock _lock = new();

    public Task SaveAsync(SessionInfo session, CancellationToken ct = default)
    { lock (_lock) _sessions[session.SessionId] = session; return Task.CompletedTask; }

    public Task<SessionInfo?> FindAsync(string sessionId, CancellationToken ct = default)
    { lock (_lock) return Task.FromResult(_sessions.TryGetValue(sessionId, out var s) ? s : null); }

    public Task DeleteAsync(string sessionId, CancellationToken ct = default)
    { lock (_lock) _sessions.Remove(sessionId); return Task.CompletedTask; }

    public Task DeleteAllForUserAsync(string userId, CancellationToken ct = default)
    {
        lock (_lock)
        {
            foreach (var k in _sessions.Where(kv => kv.Value.UserId == userId).Select(kv => kv.Key).ToList())
                _sessions.Remove(k);
        }
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<SessionInfo>> GetAllForUserAsync(string userId, CancellationToken ct = default)
    { lock (_lock) return Task.FromResult<IReadOnlyList<SessionInfo>>(_sessions.Values.Where(s => s.UserId == userId).ToList()); }
}
