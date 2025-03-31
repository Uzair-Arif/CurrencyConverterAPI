using CurrencyConverter.Application.Interfaces;
using CurrencyConverter.Application.Models.Configuration;
using CurrencyConverter.Infrastructure.DependencyInjection;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CurrencyConverter.Tests.DependencyInjection;

public class HttpClientServiceExtensionsTests
{
    [Fact]
    public void AddHttpClientServices_Should_RegisterHttpClientAndConfiguration()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
            { "ExchangeRateProviders:Frankfurter:BaseUrl", "https://api.frankfurter.app" },
            { "OpenTelemetry:Tracing:OtlpEndpoint", "http://localhost:4317" },
            { "OpenTelemetry:Tracing:ZipkinEndpoint", "http://localhost:9411/api/v2/spans" }
            })
            .Build();

        // Register caching services
        services.AddCachingServices();

        // // Register OpenTelemetry tracing
        services.AddTelemetryServices(configuration);

        // Act
        services.AddHttpClientServices(configuration);
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var httpClientFactory = serviceProvider.GetService<IHttpClientFactory>();
        httpClientFactory.Should().NotBeNull("because HttpClientFactory should be registered");

        var exchangeRateProvider = serviceProvider.GetService<IExchangeRateProvider>();
        exchangeRateProvider.Should().NotBeNull("because IExchangeRateProvider should be registered");

        var settings = serviceProvider.GetService<Microsoft.Extensions.Options.IOptions<ExchangeRateProviderSettings>>();
        settings.Should().NotBeNull("because ExchangeRateProviderSettings should be configured");
        settings!.Value.BaseUrl.Should().Be("https://api.frankfurter.app", "because it is set in the configuration");
    }
}