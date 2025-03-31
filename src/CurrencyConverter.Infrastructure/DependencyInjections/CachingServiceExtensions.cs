using CurrencyConverter.Application.Interfaces;
using CurrencyConverter.Infrastructure.Caching;
using Microsoft.Extensions.DependencyInjection;

namespace CurrencyConverter.Infrastructure.DependencyInjection;

public static class CachingServiceExtensions
{
    public static IServiceCollection AddCachingServices(this IServiceCollection services)
    {
        services.AddMemoryCache();
        services.AddSingleton<ICacheService, MemoryCacheService>();

        return services;
    }
}