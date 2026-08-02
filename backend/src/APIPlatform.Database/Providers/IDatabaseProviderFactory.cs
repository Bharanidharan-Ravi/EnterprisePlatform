using APIPlatform.Data.Options;

namespace APIPlatform.Data.Providers;

/// <summary>Resolves the correct IDatabaseProvider for a configured DatabaseOptions.Provider value.</summary>
public interface IDatabaseProviderFactory
{
    IDatabaseProvider GetProvider(DatabaseProvider kind);
}
