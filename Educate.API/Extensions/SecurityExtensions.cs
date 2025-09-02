using Educate.Application.Interfaces;
using Educate.Infrastructure.Implementations;

namespace Educate.API.Extensions;

public static class SecurityExtensions
{
    public static IServiceCollection AddSecurityServices(this IServiceCollection services)
    {
        services.AddDataProtection();

        services.AddScoped<IEncryptionService, EncryptionService>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IPaymentService, PaymentService>();

        services.AddAntiforgery(options =>
        {
            options.HeaderName = "X-CSRF-TOKEN";
            options.SuppressXFrameOptionsHeader = false;
        });

        return services;
    }
}
