using CurrencyConverter.Application.Factories;
using CurrencyConverter.Application.Interfaces;
using CurrencyConverter.Infrastructure.DependencyInjection;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace CurrencyConverter.Tests.DependencyInjection;

public class FactoryServiceExtensionsTests
{
    [Fact]
    public void AddFactoryServices_Should_RegisterFactoryDependencies()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(); // Register logging services

        // Act
        services.AddFactoryServices();
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var factory = serviceProvider.GetService<IExchangeRateProviderFactory>();
        factory.Should().NotBeNull("because IExchangeRateProviderFactory should be registered as a singleton");
        factory.Should().BeOfType<ExchangeRateProviderFactory>("because the implementation should be ExchangeRateProviderFactory");
    }
}