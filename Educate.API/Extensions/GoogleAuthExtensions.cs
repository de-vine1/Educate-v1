using Microsoft.AspNetCore.Authentication.Google;

namespace Educate.API.Extensions;

public static class GoogleAuthExtensions
{
    public static IServiceCollection AddGoogleAuthentication(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services
            .AddAuthentication()
            .AddGoogle(
                GoogleDefaults.AuthenticationScheme,
                options =>
                {
                    options.ClientId = configuration["Google:ClientId"]!;
                    options.ClientSecret = configuration["Google:ClientSecret"]!;
                    options.CallbackPath = "/api/auth/google-callback";
                }
            );

        return services;
    }
}
