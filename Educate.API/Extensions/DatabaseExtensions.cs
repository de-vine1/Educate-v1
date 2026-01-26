using Educate.Application.Interfaces;
using Educate.Infrastructure;
using Educate.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace Educate.API.Extensions;

public static class DatabaseExtensions
{
    public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                b => b.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName)));

        services.AddScoped<IAppDbContext>(provider => provider.GetRequiredService<AppDbContext>());

        return services;
    }

    // SeedDataAsync is removed as we are using EF Core HasData seeding
}
