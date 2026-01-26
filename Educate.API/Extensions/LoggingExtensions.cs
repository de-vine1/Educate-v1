using Serilog;

namespace Educate.API.Extensions;

public static class LoggingExtensions
{
    public static IServiceCollection AddSerilogLogging(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        Log.Logger = new LoggerConfiguration().ReadFrom.Configuration(configuration)
            .Enrich.FromLogContext()
            .Filter.ByExcluding(c => c.Properties.ContainsKey("RequestPath") && c.Properties["RequestPath"].ToString().Contains("/hangfire"))
            .Filter.ByExcluding(c => c.MessageTemplate.Text.Contains("hangfire/stats"))
            .CreateLogger();

        services.AddSerilog();
        return services;
    }
}
