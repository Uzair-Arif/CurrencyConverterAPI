using Microsoft.Extensions.Caching.Distributed;
using CurrencyConverter.Application.Interfaces;
using Newtonsoft.Json;

namespace CurrencyConverter.Infrastructure.Caching
{
    public class RedisCacheService : ICacheService
    {
        private readonly IDistributedCache _cache;

        public RedisCacheService(IDistributedCache cache) => _cache = cache;

        public async Task<T?> GetAsync<T>(string key)
        {
            var cachedData = await _cache.GetStringAsync(key);
            return string.IsNullOrEmpty(cachedData) ? default : JsonConvert.DeserializeObject<T>(cachedData);
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan expiration)
        {
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration
            };

            var serializedData = JsonConvert.SerializeObject(value);
            await _cache.SetStringAsync(key, serializedData, options);
        }
    }
}