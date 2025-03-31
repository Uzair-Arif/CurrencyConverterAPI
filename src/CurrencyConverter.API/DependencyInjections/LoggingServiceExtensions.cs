using Serilog;

namespace CurrencyConverter.API.DependencyInjection;

public static class LoggingServiceExtensions
{
    public static IHostBuilder AddLoggingServices(this IHostBuilder hostBuilder, IConfiguration configuration)
    {
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .CreateLogger();

        hostBuilder.UseSerilog();
        return hostBuilder;
    }
}