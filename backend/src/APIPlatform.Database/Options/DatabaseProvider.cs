namespace APIPlatform.Data.Options;

/// <summary>
/// Identifies which database engine a connection targets. Every member here has a matching
/// IDatabaseProvider implementation registered via ServiceCollectionProviderExtensions
/// (AddSqlServerProvider / AddHanaProvider) — this enum intentionally carries no placeholder
/// values for engines the platform does not yet implement.
/// </summary>
public enum DatabaseProvider
{
    SqlServer,
    Hana
}
