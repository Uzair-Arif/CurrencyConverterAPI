using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

namespace CurrencyConverter.Infrastructure.DependencyInjection;

public static class OpenTelemetryServiceExtensions
{
    public static IServiceCollection AddTelemetryServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOpenTelemetry()
            .WithTracing(tracing =>
            {
                tracing
                    .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("CurrencyConverterAPI"))
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddOtlpExporter(options => options.Endpoint = new Uri(configuration["OpenTelemetry:Tracing:OtlpEndpoint"]))
                    .AddZipkinExporter(options => options.Endpoint = new Uri(configuration["OpenTelemetry:Tracing:ZipkinEndpoint"]));
            });

        return services;
    }
}