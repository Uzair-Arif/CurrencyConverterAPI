using CurrencyConverter.Infrastructure.Caching;
using Microsoft.Extensions.Caching.Memory;
using NSubstitute;
using FluentAssertions;

namespace CurrencyConverter.Tests.Infrastructure.Caching
{
    public class MemoryCacheServiceTests
    {
        private readonly IMemoryCache _memoryCache;
        private readonly MemoryCacheService _cacheService;

        public MemoryCacheServiceTests()
        {
            _memoryCache = Substitute.For<IMemoryCache>();
            _cacheService = new MemoryCacheService(_memoryCache);
        }

        [Fact]
        public async Task GetAsync_ShouldReturnCachedValue_WhenKeyExists()
        {
            // Arrange
            var key = "test-key";
            var expectedValue = "cached-value";
            object cacheEntry = expectedValue;

            _memoryCache.TryGetValue(key, out Arg.Any<object>())
                        .Returns(call =>
                        {
                            call[1] = cacheEntry; // Simulate setting the output parameter
                            return true;
                        });

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

            _memoryCache.TryGetValue(key, out Arg.Any<object>())
                        .Returns(false);

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

            // Act
            await _cacheService.SetAsync(key, value, expiration);

            // Assert
            _memoryCache.Received(1).Set(key, value, expiration);
        }
    }
}