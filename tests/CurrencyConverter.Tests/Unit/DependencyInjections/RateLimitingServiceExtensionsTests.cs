using AspNetCoreRateLimit;
using CurrencyConverter.API.DependencyInjection;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CurrencyConverter.Tests.DependencyInjection;

public class RateLimitingServiceExtensionsTests
{
    [Fact]
    public void AddRateLimitingServices_Should_RegisterRateLimitingDependencies()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                { "IpRateLimiting:EnableEndpointRateLimiting", "true" },
                { "IpRateLimiting:StackBlockedRequests", "false" },
                { "IpRateLimiting:RealIpHeader", "X-Real-IP" }
            })
            .Build();

        // Act
        services.AddRateLimitingServices(configuration);
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        serviceProvider.GetService<IIpPolicyStore>().Should().NotBeNull("because IIpPolicyStore should be registered");
        serviceProvider.GetService<IRateLimitCounterStore>().Should().NotBeNull("because IRateLimitCounterStore should be registered");
        serviceProvider.GetService<IRateLimitConfiguration>().Should().NotBeNull("because IRateLimitConfiguration should be registered");
        serviceProvider.GetService<IProcessingStrategy>().Should().NotBeNull("because IProcessingStrategy should be registered");
    }

    [Fact]
    public void AddRateLimitingServices_Should_ConfigureIpRateLimitOptions()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                { "IpRateLimiting:EnableEndpointRateLimiting", "true" },
                { "IpRateLimiting:StackBlockedRequests", "false" },
                { "IpRateLimiting:RealIpHeader", "X-Real-IP" }
            })
            .Build();

        // Act
        services.AddRateLimitingServices(configuration);
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var options = serviceProvider.GetService<Microsoft.Extensions.Options.IOptions<IpRateLimitOptions>>();
        options.Should().NotBeNull("because IpRateLimitOptions should be configured");
        options!.Value.EnableEndpointRateLimiting.Should().BeTrue("because it is set to true in the configuration");
        options.Value.StackBlockedRequests.Should().BeFalse("because it is set to false in the configuration");
        options.Value.RealIpHeader.Should().Be("X-Real-IP", "because it is set in the configuration");
    }
}