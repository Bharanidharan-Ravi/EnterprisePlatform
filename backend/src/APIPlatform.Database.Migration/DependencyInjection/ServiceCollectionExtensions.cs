using APIPlatform.Database.Migration.Abstractions;
using APIPlatform.Database.Migration.Schema.Abstractions;
using APIPlatform.Database.Migration.Schema.Services;
using APIPlatform.Database.Migration.Services;
using APIPlatform.Database.Migration.Sql.Dialects;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace APIPlatform.Database.Migration.DependencyInjection;

/// <summary>
/// Registers the core migration engine. Follows the platform-wide AddXxx() DI convention.
/// Requires <c>AddDatabase(...)</c> (with a matching <c>AddSqlServerProvider()</c>/
/// <c>AddHanaProvider()</c>) and an <c>IClock</c> registration to already be present — this
/// method registers neither, since both are shared platform concerns owned by their own modules
/// (same convention <c>AddNotification()</c> follows).
///
/// <para>Registers no <see cref="IMigration"/> itself. Versioned migrations that must ship with a
/// release are registered by whichever module owns them, via its own additive
/// <c>AddXxxMigrations()</c> extension, so adding one never requires a change here.</para>
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDatabaseMigration(this IServiceCollection services)
    {
        // TryAdd: AddSchemaMigration() registers the same resolver, and an app calling both should
        // end up with one registration, not two.
        services.TryAddScoped<IMigrationSqlDialectResolver, MigrationSqlDialectResolver>();
        services.AddScoped<IMigrationHistoryRepository, MigrationHistoryRepository>();
        services.AddScoped<IMigrationRunner, MigrationRunner>();

        return services;
    }

    /// <summary>
    /// Registers the runtime schema engine — create/update/delete tables from a request body,
    /// using the predefined table templates plus whatever extra fields a caller supplies.
    /// Separate from <see cref="AddDatabaseMigration"/> because it is a genuinely different
    /// mechanism (no version history; reads the live catalog instead) and because it is
    /// privileged: anything that can reach <see cref="ISchemaMigrationService"/> can drop a table,
    /// so an app opts into it deliberately rather than getting it by default.
    /// </summary>
    public static IServiceCollection AddSchemaMigration(this IServiceCollection services)
    {
        services.TryAddScoped<IMigrationSqlDialectResolver, MigrationSqlDialectResolver>();
        services.AddScoped<ISchemaMigrationService, SchemaMigrationService>();

        return services;
    }
}
