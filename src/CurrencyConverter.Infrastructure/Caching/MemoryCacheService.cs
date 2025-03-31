using Microsoft.Extensions.Caching.Memory;
using CurrencyConverter.Application.Interfaces;

namespace CurrencyConverter.Infrastructure.Caching
{
    public class MemoryCacheService : ICacheService
    {
        private readonly IMemoryCache _cache;

        public MemoryCacheService(IMemoryCache cache) => _cache = cache;

        public Task<T?> GetAsync<T>(string key) =>
            Task.FromResult(_cache.TryGetValue(key, out T value) ? value : default);

        public Task SetAsync<T>(string key, T value, TimeSpan expiration)
        {
            _cache.Set(key, value, expiration);
            return Task.CompletedTask;
        }
    }
}