using Educate.Application.Interfaces;
using Educate.Infrastructure.Implementations;

namespace Educate.API.Extensions;

public static class ApplicationExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Register application service interfaces with their implementations
        services.AddScoped<ISubscriptionService, SubscriptionService>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<INotificationService, NotificationService>();

        return services;
    }
}
