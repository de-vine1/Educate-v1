using Educate.Application.Interfaces;
using Educate.Infrastructure.Implementations;
using Educate.Infrastructure.Services;


namespace Educate.API.Extensions;

public static class EmailExtensions
{
    public static IServiceCollection AddEmailServices(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {

        services.AddHttpClient<EmailService>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<IUserValidationService, UserValidationService>();
        services.AddScoped<IJwtService, JwtService>();
        services.AddSingleton<RateLimitingService>();
        services.AddMemoryCache();

        return services;
    }
}
