namespace APIPlatform.Authentication.Context;

/// <summary>Scoped per-request container. Every EnterprisePlatform module depends on this
/// instead of IHttpContextAccessor. Set once by CurrentUserContextMiddleware at the start of
/// each request; read by any downstream service that needs identity.</summary>
public interface ICurrentUserContextAccessor
{
    /// <summary>Returns Anonymous (never null) when no authenticated user exists.</summary>
    ICurrentUserContext Current { get; }
    void Set(ICurrentUserContext context);
    void Clear();
}

/// <summary>Default scoped implementation — a simple in-memory holder per request lifetime.</summary>
public sealed class CurrentUserContextAccessor : ICurrentUserContextAccessor
{
    private ICurrentUserContext _current = CurrentUserContext.Anonymous;
    public ICurrentUserContext Current => _current;
    public void Set(ICurrentUserContext context) => _current = context;
    public void Clear() => _current = CurrentUserContext.Anonymous;
}
