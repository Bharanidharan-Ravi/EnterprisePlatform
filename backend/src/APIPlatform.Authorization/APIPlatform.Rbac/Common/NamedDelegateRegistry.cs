using System.Collections.Concurrent;

namespace APIPlatform.Rbac.Common;

/// <summary>
/// Generic named-delegate registry shared by PolicyRuleRegistry and RowFilterRegistry — both
/// follow the identical "register by name, resolve by name" extension-point shape (Section 7.2
/// of the Master Plan), so the mechanics live here once (DRY) instead of being duplicated.
/// </summary>
public class NamedDelegateRegistry<TDelegate> where TDelegate : Delegate
{
    private readonly ConcurrentDictionary<string, TDelegate> _registrations =
        new(StringComparer.OrdinalIgnoreCase);

    public void Register(string name, TDelegate handler) => _registrations[name] = handler;

    public bool TryResolve(string name, out TDelegate? handler) =>
        _registrations.TryGetValue(name, out handler);
}
