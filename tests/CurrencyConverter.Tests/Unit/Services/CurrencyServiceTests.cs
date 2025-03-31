using CurrencyConverter.Application.Interfaces;
using CurrencyConverter.Application.Models.Response;
using CurrencyConverter.Application.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace CurrencyConverter.Tests.Services
{
    public class CurrencyServiceTests
    {
        private readonly IExchangeRateProviderFactory _providerFactory;
        private readonly IExchangeRateProvider _exchangeRateProvider;
        private readonly ILogger<CurrencyService> _logger;
        private readonly CurrencyService _currencyService;

        public CurrencyServiceTests()
        {
            _providerFactory = Substitute.For<IExchangeRateProviderFactory>();
            _exchangeRateProvider = Substitute.For<IExchangeRateProvider>();
            _logger = Substitute.For<ILogger<CurrencyService>>();
            _currencyService = new CurrencyService(_providerFactory, _logger);
        }

        #region GetLatestRatesAsync Tests

        [Fact]
        public async Task GetLatestRatesAsync_ShouldReturnRates_WhenRatesExist()
        {
            // Arrange
            var baseCurrency = "USD";
            var expectedRates = new ExchangeRateResponse
            {
                BaseCurrency = baseCurrency,
                Rates = new Dictionary<string, decimal> { { "EUR", 0.85m } }
            };

            _providerFactory.GetProvider(Arg.Any<string>()).Returns(_exchangeRateProvider);
            _exchangeRateProvider.GetLatestRatesAsync(baseCurrency, null).Returns(Task.FromResult(expectedRates));

            // Act
            var result = await _currencyService.GetLatestRatesAsync(baseCurrency);

            // Assert
            result.Should().NotBeNull();
            result.BaseCurrency.Should().Be(baseCurrency);
            result.Rates.Should().ContainKey("EUR");
            result.Rates["EUR"].Should().Be(0.85m);
        }

        [Fact]
        public async Task GetLatestRatesAsync_ShouldThrowKeyNotFoundException_WhenNoRatesExist()
        {
            // Arrange
            var baseCurrency = "USD";
            _providerFactory.GetProvider(Arg.Any<string>()).Returns(_exchangeRateProvider);
            _exchangeRateProvider.GetLatestRatesAsync(baseCurrency, null).Returns(Task.FromResult<ExchangeRateResponse>(null));

            // Act
            Func<Task> act = async () => await _currencyService.GetLatestRatesAsync(baseCurrency);

            // Assert
            await act.Should().ThrowAsync<KeyNotFoundException>()
                .WithMessage($"No exchange rates found for {baseCurrency}.");
        }

        [Fact]
        public async Task GetLatestRatesAsync_UnhandledException_ThrowsInvalidOperationException()
        {
            // Arrange
            var baseCurrency = "USD";
            var targetCurrency = "EUR";
            var provider = "FrankfurterAPI";

            _providerFactory.GetProvider(provider)
                .GetLatestRatesAsync(baseCurrency, targetCurrency)
                .Throws(new Exception("Unexpected error while getting exchange rates"));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _currencyService.GetLatestRatesAsync(baseCurrency, targetCurrency, provider));

            exception.Message.Should().Be("An error occurred while fetching exchange rates. Please try again later.");
        }

        #endregion

        #region ConvertCurrencyAsync Tests

        [Fact]
        public async Task ConvertCurrencyAsync_ShouldReturnConvertedAmount_WhenRatesExist()
        {
            // Arrange
            var from = "USD";
            var to = "EUR";
            decimal amount = 100m;
            var expectedRate = 0.85m;
            var expectedResponse = new ExchangeRateResponse
            {
                BaseCurrency = from,
                Rates = new Dictionary<string, decimal> { { to, expectedRate } }
            };

            _providerFactory.GetProvider(Arg.Any<string>()).Returns(_exchangeRateProvider);
            _exchangeRateProvider.GetLatestRatesAsync(baseCurrency: from, targetCurrency: to).Returns(Task.FromResult(expectedResponse));

            // Act
            var result = await _currencyService.ConvertCurrencyAsync(from, to, amount, "FrankfurterAPI");

            // Assert
            result.Should().NotBeNull();
            result.From.Should().Be(from);
            result.To.Should().Be(to);
            result.ConvertedAmount.Should().Be(amount * expectedRate);
        }

        [Fact]
        public async Task ConvertCurrencyAsync_ShouldThrowKeyNotFoundException_WhenBaseCurrencyHasNoRates()
        {
            // Arrange
            var from = "USD";
            var to = "EUR";
            decimal amount = 100m;

            _providerFactory.GetProvider(Arg.Any<string>()).Returns(_exchangeRateProvider);
            _exchangeRateProvider.GetLatestRatesAsync(baseCurrency: from, targetCurrency: to).Returns(Task.FromResult<ExchangeRateResponse>(null));

            // Act
            Func<Task> act = async () => await _currencyService.ConvertCurrencyAsync(from, to, amount, "FrankfurterAPI");

            // Assert
            await act.Should().ThrowAsync<KeyNotFoundException>()
                 .WithMessage("No exchange rates found for USD.");
        }

        [Fact]
        public async Task ConvertCurrencyAsync_ShouldThrowKeyNotFoundException_WhenTargetCurrencyIsNotFound()
        {
            // Arrange
            var from = "USD";
            var to = "EUR";
            decimal amount = 100m;
            var ratesResponse = new ExchangeRateResponse
            {
                BaseCurrency = from,
                Rates = new Dictionary<string, decimal>() { { "GBP", 0.75m } } // No EUR rate
            };

            _providerFactory.GetProvider(Arg.Any<string>()).Returns(_exchangeRateProvider);
            _exchangeRateProvider.GetLatestRatesAsync(baseCurrency: from, targetCurrency: to).Returns(Task.FromResult(ratesResponse));

            // Act
            Func<Task> act = async () => await _currencyService.ConvertCurrencyAsync(from, to, amount, "FrankfurterAPI");

            // Assert
            await act.Should().ThrowAsync<KeyNotFoundException>()
                .WithMessage($"Exchange rate not found for conversion from '{from}' to '{to}'.");
        }

        [Fact]
        public async Task ConvertCurrencyAsync_ShouldThrowArgumentException_WhenExcludedCurrencyIsUsed()
        {
            // Arrange
            var from = "TRY"; // Excluded currency
            var to = "USD";
            decimal amount = 100m;

            // Act
            Func<Task> act = async () => await _currencyService.ConvertCurrencyAsync(from, to, amount, "FrankfurterAPI");

            // Assert
            await act.Should().ThrowAsync<ArgumentException>()
                .WithMessage($"Conversion involving {from} or {to} is not allowed.");
        }

        [Fact]
        public async Task ConvertCurrencyAsync_UnhandledException_ThrowsInvalidOperationException()
        {
            // Arrange
            var from = "USD";
            var to = "EUR";
            var amount = 100m;
            var provider = "FrankfurterAPI";

            _providerFactory.GetProvider(provider)
                .GetLatestRatesAsync(from, to)
                .Throws(new Exception("Unexpected error while getting exchange rates"));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _currencyService.ConvertCurrencyAsync(from, to, amount, provider));

            exception.Message.Should().Be("An error occurred while converting currency. Please try again later.");
        }

        #endregion

        #region GetHistoricalRatesAsync Tests

        [Fact]
        public async Task GetHistoricalRatesAsync_ShouldReturnPaginatedResults_WhenRatesExist()
        {
            // Arrange
            var baseCurrency = "USD";
            var startDate = DateTime.UtcNow.AddDays(-10);
            var endDate = DateTime.UtcNow;
            int page = 1;
            int pageSize = 2;

            var historicalRates = new HistoricalExchangeRateResponse
            {
                BaseCurrency = baseCurrency,
                StartDate = startDate,
                EndDate = endDate,
                Rates = new Dictionary<DateTime, Dictionary<string, decimal>>
                {
                    { startDate, new Dictionary<string, decimal> { { "EUR", 0.85m } } },
                    { startDate.AddDays(1), new Dictionary<string, decimal> { { "EUR", 0.86m } } },
                    { startDate.AddDays(2), new Dictionary<string, decimal> { { "EUR", 0.87m } } }
                }
            };

            _providerFactory.GetProvider(Arg.Any<string>()).Returns(_exchangeRateProvider);
            _exchangeRateProvider.GetHistoricalRatesAsync(startDate, endDate, baseCurrency).Returns(Task.FromResult(historicalRates));

            // Act
            var result = await _currencyService.GetHistoricalRatesAsync(startDate, endDate, baseCurrency, page, pageSize, "FrankfurterAPI");

            // Assert
            result.Should().NotBeNull();
            result.Rates.Should().HaveCount(2);
        }

        [Fact]
        public async Task GetHistoricalRatesAsync_ShouldThrowKeyNotFoundException_WhenNoRatesExist()
        {
            // Arrange
            var baseCurrency = "USD";
            var startDate = DateTime.UtcNow.AddDays(-10);
            var endDate = DateTime.UtcNow;

            _providerFactory.GetProvider(Arg.Any<string>()).Returns(_exchangeRateProvider);
            _exchangeRateProvider.GetHistoricalRatesAsync(startDate, endDate, baseCurrency).Returns(Task.FromResult<HistoricalExchangeRateResponse>(null));

            // Act
            Func<Task> act = async () => await _currencyService.GetHistoricalRatesAsync(startDate, endDate, baseCurrency, 1, 2, "FrankfurterAPI");

            // Assert
            await act.Should().ThrowAsync<KeyNotFoundException>()
                .WithMessage("No historical exchange rates found.");
        }

        [Fact]
        public async Task GetHistoricalRatesAsync_UnhandledException_ThrowsInvalidOperationException()
        {
            // Arrange
            var startDate = DateTime.Now.AddDays(-30);
            var endDate = DateTime.Now;
            var baseCurrency = "USD";
            var page = 1;
            var pageSize = 10;
            var provider = "FrankfurterAPI";

            _providerFactory.GetProvider(provider)
                .GetHistoricalRatesAsync(startDate, endDate, baseCurrency)
                .Throws(new Exception("Unexpected error while getting historical rates"));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _currencyService.GetHistoricalRatesAsync(startDate, endDate, baseCurrency, page, pageSize, provider));

            exception.Message.Should().Be("An error occurred while fetching historical exchange rates. Please try again later.");
        }

        #endregion
    }
}