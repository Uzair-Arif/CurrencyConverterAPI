using CurrencyConverter.Application.Factories;
using CurrencyConverter.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace CurrencyConverter.Infrastructure.DependencyInjection;

public static class FactoryServiceExtensions
{
    public static IServiceCollection AddFactoryServices(this IServiceCollection services)
    {
        services.AddSingleton<IExchangeRateProviderFactory, ExchangeRateProviderFactory>();

        return services;
    }
}