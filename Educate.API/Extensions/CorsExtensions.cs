namespace Educate.API.Extensions;

public static class CorsExtensions
{
    public static IServiceCollection AddCorsPolicy(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services.AddCors(options =>
        {
            options.AddPolicy(
                "DefaultPolicy",
                policy =>
                {
                    var allowedOrigins =
                        configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? ["*"];

                    if (allowedOrigins.Contains("*"))
                    {
                        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
                    }
                    else
                    {
                        policy
                            .WithOrigins(allowedOrigins)
                            .AllowAnyMethod()
                            .AllowAnyHeader()
                            .AllowCredentials();
                    }
                }
            );
        });

        return services;
    }
}
