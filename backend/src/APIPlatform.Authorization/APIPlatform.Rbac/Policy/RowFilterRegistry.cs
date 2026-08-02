using APIPlatform.Rbac.Common;
using APIPlatform.Rbac.Contexts;
using APIPlatform.Rbac.Contracts;

namespace APIPlatform.Rbac.Policy;

public sealed class RowFilterRegistry
    : NamedDelegateRegistry<Func<AuthorizationContext, Task<RowFilterDescriptor>>>, IRowFilterRegistry
{
}
