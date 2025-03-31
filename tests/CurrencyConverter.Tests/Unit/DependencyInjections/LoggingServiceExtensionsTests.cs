using CurrencyConverter.API.DependencyInjection;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace CurrencyConverter.Tests.DependencyInjection;

public class LoggingServiceExtensionsTests
{
    [Fact]
    public void AddLoggingServices_Should_ConfigureSerilog()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                { "Serilog:MinimumLevel:Default", "Information" },
                { "Serilog:WriteTo:0:Name", "Console" }
            })
            .Build();

        var hostBuilder = new HostBuilder();

        // Act
        hostBuilder.AddLoggingServices(configuration);

        // Assert
        Log.Logger.Should().NotBeNull("because Serilog should be configured");
        Log.Logger.Should().BeOfType<Serilog.Core.Logger>("because Serilog should be the active logger");
    }
    [Fact]
    public void AddLoggingServices_WithMissingConfiguration_ShouldNotThrow()
    {
        // Arrange
        var configuration = new ConfigurationBuilder().Build(); // No configuration provided
        var hostBuilder = new HostBuilder();

        // Act
        var exception = Record.Exception(() => hostBuilder.AddLoggingServices(configuration));

        // Assert
        exception.Should().BeNull("because the method should handle missing configuration gracefully");
    }
}