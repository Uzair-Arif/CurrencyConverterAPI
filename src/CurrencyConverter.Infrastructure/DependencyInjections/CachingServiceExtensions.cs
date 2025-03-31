using CurrencyConverter.Application.Interfaces;
using CurrencyConverter.Infrastructure.Caching;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CurrencyConverter.Infrastructure.DependencyInjection;

public static class CachingServiceExtensions
{
    public static IServiceCollection AddCachingServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = configuration.GetSection("Redis:ConnectionString").Value;
            });
        services.AddSingleton<ICacheService, RedisCacheService>();

        return services;
    }
}