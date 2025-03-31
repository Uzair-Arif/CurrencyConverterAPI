using CurrencyConverter.Application;
using CurrencyConverter.Application.Interfaces;
using CurrencyConverter.Application.Models.Request;
using FluentAssertions;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CurrencyConverter.Tests.DependencyInjection;

public class ApplicationDependencyInjectionTests
{
    [Fact]
    public void AddApplicationServices_Should_RegisterApplicationDependencies()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(); // Register logging services

        // Add in-memory configuration
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
            { "JwtSettings:Secret", "supersecretkey12345" },
            { "JwtSettings:Issuer", "TestIssuer" },
            { "JwtSettings:Audience", "TestAudience" }
            })
            .Build();
        services.AddSingleton<IConfiguration>(configuration);

        // Act
        services.AddApplicationServices();
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        // Verify services
        serviceProvider.GetService<ICurrencyService>().Should().NotBeNull("because ICurrencyService should be registered");
        serviceProvider.GetService<ITokenService>().Should().NotBeNull("because ITokenService should be registered");
        serviceProvider.GetService<IExchangeRateProviderFactory>().Should().NotBeNull("because IExchangeRateProviderFactory should be registered as a singleton");

        // Verify validators
        serviceProvider.GetService<IValidator<ConvertCurrencyRequest>>().Should().NotBeNull("because ConvertCurrencyRequestValidator should be registered");
        serviceProvider.GetService<IValidator<HistoricalExchangeRateRequest>>().Should().NotBeNull("because HistoricalExchangeRateRequestValidator should be registered");
        serviceProvider.GetService<IValidator<GetRatesRequest>>().Should().NotBeNull("because GetRatesRequestValidator should be registered");
    }
}