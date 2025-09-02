namespace Educate.API.Extensions;

public static class AuthorizationExtensions
{
    public static IServiceCollection AddAuthorizationPolicies(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
            options.AddPolicy("InstructorOnly", policy => policy.RequireRole("Instructor"));
            options.AddPolicy("StudentOnly", policy => policy.RequireRole("Student"));
            options.AddPolicy(
                "AdminOrInstructor",
                policy => policy.RequireRole("Admin", "Instructor")
            );
        });

        return services;
    }
}
