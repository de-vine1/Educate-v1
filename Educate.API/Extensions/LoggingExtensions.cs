using Serilog;

namespace Educate.API.Extensions;

public static class LoggingExtensions
{
    public static IServiceCollection AddSerilogLogging(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        Log.Logger = new LoggerConfiguration().ReadFrom.Configuration(configuration).CreateLogger();

        services.AddSerilog();
        return services;
    }
}
