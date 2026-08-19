using APIPlatform.Database.Migration.Abstractions;
using APIPlatform.Database.Migration.Migrations.Notification;
using Microsoft.Extensions.DependencyInjection;

namespace APIPlatform.Database.Migration.DependencyInjection;

/// <summary>
/// Registers the initial APIPlatform.Notification schema migration (both provider variants —
/// <see cref="IMigrationRunner"/> applies only the one matching the configured provider). Kept
/// as its own extension method, separate from <c>AddDatabaseMigration()</c>, so registering one
/// module's migrations never requires touching the core engine's registration — the same pattern
/// a future <c>APIPlatform.Database.StoredProcedures</c> package (or any other schema-owning
/// module) would follow with its own <c>AddXxxMigrations()</c>.
/// </summary>
public static class NotificationMigrationServiceCollectionExtensions
{
    public static IServiceCollection AddNotificationSchemaMigrations(this IServiceCollection services)
    {
        services.AddScoped<IMigration, NotificationSqlServerMigration>();
        services.AddScoped<IMigration, NotificationHanaMigration>();

        return services;
    }
}
