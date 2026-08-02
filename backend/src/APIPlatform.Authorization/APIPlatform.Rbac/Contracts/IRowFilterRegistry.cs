using APIPlatform.Rbac.Contexts;

namespace APIPlatform.Rbac.Contracts;

/// <summary>Extension point: consuming apps register named row-filter-builder delegates here.</summary>
public interface IRowFilterRegistry
{
    void Register(string name, Func<AuthorizationContext, Task<RowFilterDescriptor>> builder);
    bool TryResolve(string name, out Func<AuthorizationContext, Task<RowFilterDescriptor>>? builder);
}
