using CurrencyConverter.Infrastructure.Caching;
using NSubstitute;
using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using System.Text;
using Newtonsoft.Json;

namespace CurrencyConverter.Tests.Infrastructure.Caching;

public class MemoryCacheServiceTests
{
    private readonly IDistributedCache _redisCache;
    private readonly RedisCacheService _cacheService;

    public MemoryCacheServiceTests()
    {
        _redisCache = Substitute.For<IDistributedCache>();
        _cacheService = new RedisCacheService(_redisCache);
    }

    [Fact]
    public async Task GetAsync_ShouldReturnCachedValue_WhenKeyExists()
    {
        // Arrange
        var key = "test-key";
        var expectedValue = "cached-value";
        var serializedValue = JsonConvert.SerializeObject(expectedValue); // Serialize value as JSON
        var expectedBytes = Encoding.UTF8.GetBytes(serializedValue); // Convert to byte array

        _redisCache.GetAsync(key, Arg.Any<CancellationToken>())
                   .Returns(Task.FromResult(expectedBytes)); // Return JSON byte array

        // Act
        var result = await _cacheService.GetAsync<string>(key);

        // Assert
        result.Should().Be(expectedValue);
    }

    [Fact]
    public async Task GetAsync_ShouldReturnNull_WhenKeyDoesNotExist()
    {
        // Arrange
        var key = "non-existent-key";

        _redisCache.GetAsync(key, Arg.Any<CancellationToken>())
                   .Returns(Task.FromResult<byte[]>(null)); // Return null byte[]

        // Act
        var result = await _cacheService.GetAsync<string>(key);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task SetAsync_ShouldStoreValueInCache()
    {
        // Arrange
        var key = "test-key";
        var value = "cached-value";
        var expiration = TimeSpan.FromMinutes(5);

        var serializedValue = JsonConvert.SerializeObject(value); // Serialize to JSON
        var expectedBytes = Encoding.UTF8.GetBytes(serializedValue); // Convert to byte array

        // Act
        await _cacheService.SetAsync(key, value, expiration);

        // Assert
        await _redisCache.Received(1).SetAsync(
            key,
            Arg.Is<byte[]>(v => v.SequenceEqual(expectedBytes)), // Verify stored value matches serialized bytes
            Arg.Is<DistributedCacheEntryOptions>(o => o.AbsoluteExpirationRelativeToNow == expiration),
            Arg.Any<CancellationToken>());
    }
}
