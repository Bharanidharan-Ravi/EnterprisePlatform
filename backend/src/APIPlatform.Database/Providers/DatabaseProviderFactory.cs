using APIPlatform.Data.Exceptions;
using APIPlatform.Data.Options;
using APIPlatform.Foundation.Exceptions;

namespace APIPlatform.Data.Providers;

/// <summary>Default IDatabaseProviderFactory — resolves by matching each registered IDatabaseProvider's Kind.</summary>
public sealed class DatabaseProviderFactory : IDatabaseProviderFactory
{
    private readonly IReadOnlyDictionary<DatabaseProvider, IDatabaseProvider> _providers;

    public DatabaseProviderFactory(IEnumerable<IDatabaseProvider> providers) =>
        _providers = providers.ToDictionary(p => p.Kind);

    public IDatabaseProvider GetProvider(DatabaseProvider kind) =>
        _providers.TryGetValue(kind, out var provider)
            ? provider
            : throw new DatabaseException($"No IDatabaseProvider is registered for '{kind}'.", ErrorCategory.Infrastructure);
}
