using APIPlatform.Notification.Abstractions;
using APIPlatform.Notification.Repositories;
using APIPlatform.Notification.Services;
using APIPlatform.Notification.Sql.Dialects;
using Microsoft.Extensions.DependencyInjection;

namespace APIPlatform.Notification.DependencyInjection;

/// <summary>
/// Registers APIPlatform.Notification. Follows the platform-wide AddXxx() DI convention.
/// Requires <c>AddDatabase(...)</c> (and a matching <c>AddSqlServerProvider()</c>/<c>AddHanaProvider()</c>)
/// and an <c>IClock</c> registration to already be present in the container — Notification does
/// not register either itself, since both are shared platform concerns owned by their own modules.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddNotification(this IServiceCollection services)
    {
        services.AddScoped<INotificationSqlDialectResolver, NotificationSqlDialectResolver>();
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<INotificationService, NotificationService>();

        return services;
    }
}
