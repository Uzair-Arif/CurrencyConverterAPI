using CurrencyConverter.Application.Interfaces;
using CurrencyConverter.Application.Models.Configuration;
using CurrencyConverter.Infrastructure.Providers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CurrencyConverter.Infrastructure.DependencyInjection;

public static class HttpClientServiceExtensions
{
    public static IServiceCollection AddHttpClientServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpClient<IExchangeRateProvider, FrankfurterExchangeRateProvider>();
        services.Configure<ExchangeRateProviderSettings>(
            configuration.GetSection("ExchangeRateProviders:Frankfurter"));

        return services;
    }
}