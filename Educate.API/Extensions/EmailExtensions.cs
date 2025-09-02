using Educate.Application.Interfaces;
using Educate.Infrastructure.Implementations;
using Educate.Infrastructure.Services;
using SendGrid;

namespace Educate.API.Extensions;

public static class EmailExtensions
{
    public static IServiceCollection AddEmailServices(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        var apiKey = configuration["SendGrid:ApiKey"];
        services.AddSingleton<ISendGridClient>(_ => new SendGridClient(apiKey));
        services.AddScoped<IEmailService, SendGridEmailService>();
        services.AddScoped<IUserValidationService, UserValidationService>();
        services.AddScoped<IJwtService, JwtService>();
        services.AddSingleton<RateLimitingService>();
        services.AddMemoryCache();

        return services;
    }
}
