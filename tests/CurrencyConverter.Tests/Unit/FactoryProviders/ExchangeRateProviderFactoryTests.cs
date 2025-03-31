using CurrencyConverter.Application.Factories;
using CurrencyConverter.Application.Interfaces;
using Microsoft.Extensions.Logging;
using NSubstitute;
using FluentAssertions;

namespace CurrencyConverter.Tests.Factories
{
    public class ExchangeRateProviderFactoryTests
    {
        private readonly ILogger<ExchangeRateProviderFactory> _logger;
        private readonly IExchangeRateProvider _mockProvider1;
        private readonly IExchangeRateProvider _mockProvider2;
        private readonly ExchangeRateProviderFactory _factory;

        public ExchangeRateProviderFactoryTests()
        {
            _logger = Substitute.For<ILogger<ExchangeRateProviderFactory>>();

            _mockProvider1 = Substitute.For<IExchangeRateProvider>();
            _mockProvider1.ProviderName.Returns("FrankfurterAPI");

            _mockProvider2 = Substitute.For<IExchangeRateProvider>();
            _mockProvider2.ProviderName.Returns("OpenExchangeRates");

            var providers = new List<IExchangeRateProvider> { _mockProvider1, _mockProvider2 };

            _factory = new ExchangeRateProviderFactory(providers, _logger);
        }

        [Fact]
        public void GetProvider_ShouldReturnCorrectProvider_WhenProviderExists()
        {
            // Act
            var provider = _factory.GetProvider("FrankfurterAPI");

            // Assert
            provider.Should().Be(_mockProvider1);
        }

        [Fact]
        public void GetProvider_ShouldLogInformation_WhenValidProviderIsRetrieved()
        {
            // Act
            _factory.GetProvider("OpenExchangeRates");

            // Assert
            _logger.Received(1).Log(
                LogLevel.Information,
                Arg.Any<EventId>(),
                Arg.Is<object>(o => o.ToString()!.Contains("Using OpenExchangeRates for exchange rates.")),
                Arg.Any<Exception>(),
                Arg.Any<Func<object, Exception?, string>>());
        }

        [Fact]
        public void GetProvider_ShouldThrowArgumentException_WhenProviderDoesNotExist()
        {
            // Act
            Action act = () => _factory.GetProvider("NonExistentProvider");

            // Assert
            act.Should().Throw<ArgumentException>()
                .WithMessage("No provider found for NonExistentProvider");
        }

        [Fact]
        public void GetProvider_ShouldIgnoreCase_WhenMatchingProviderNames()
        {
            // Act
            var provider = _factory.GetProvider("frankfurterapi"); // Lowercase input

            // Assert
            provider.Should().Be(_mockProvider1);
        }
    }
}