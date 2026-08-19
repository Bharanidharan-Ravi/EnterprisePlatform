using APIPlatform.Database.Migration.Abstractions;
using APIPlatform.Database.Migration.Services;
using APIPlatform.Database.Migration.Sql.Dialects;
using Microsoft.Extensions.DependencyInjection;

namespace APIPlatform.Database.Migration.DependencyInjection;

/// <summary>
/// Registers the core migration engine. Follows the platform-wide AddXxx() DI convention.
/// Requires <c>AddDatabase(...)</c> (with a matching <c>AddSqlServerProvider()</c>/
/// <c>AddHanaProvider()</c>) and an <c>IClock</c> registration to already be present — this
/// method registers neither, since both are shared platform concerns owned by their own modules
/// (same convention <c>AddNotification()</c> follows).
///
/// Registers no <see cref="IMigration"/> itself — call the migration-set extension(s) you need
/// afterward (e.g. <c>AddNotificationSchemaMigrations()</c>), each additive and independent, so a
/// future stored-procedure or other schema package can register its own migrations the same way
/// with zero change here.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDatabaseMigration(this IServiceCollection services)
    {
        services.AddScoped<IMigrationSqlDialectResolver, MigrationSqlDialectResolver>();
        services.AddScoped<IMigrationHistoryRepository, MigrationHistoryRepository>();
        services.AddScoped<IMigrationRunner, MigrationRunner>();

        return services;
    }
}
