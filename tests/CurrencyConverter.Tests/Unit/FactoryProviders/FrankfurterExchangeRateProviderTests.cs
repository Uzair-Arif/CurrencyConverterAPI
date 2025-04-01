using System.Net;
using System.Text.Json;
using CurrencyConverter.Application.Interfaces;
using CurrencyConverter.Application.Models.Configuration;
using CurrencyConverter.Application.Models.Response;
using CurrencyConverter.Infrastructure.Providers;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using OpenTelemetry.Trace;
using Polly.CircuitBreaker;

namespace CurrencyConverter.Tests.Infrastructure.Providers;

public class FrankfurterExchangeRateProviderTests
{
    private HttpClient _httpClient;
    private ICacheService _cacheService;
    private ILogger<FrankfurterExchangeRateProvider> _logger;
    private TracerProvider _tracerProvider;
    private IOptions<ExchangeRateProviderSettings> _options;
    private FakeHttpMessageHandler _fakeHandler;
    private FrankfurterExchangeRateProvider _provider;

    public FrankfurterExchangeRateProviderTests()
    {
        ResetDependencies(); // Re-initialize before each test
    }

    private void ResetDependencies()
    {
        _fakeHandler = new FakeHttpMessageHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));
        _httpClient = new HttpClient(_fakeHandler) { BaseAddress = new Uri("https://api.frankfurter.app/") };
        _logger = Substitute.For<ILogger<FrankfurterExchangeRateProvider>>();
        _cacheService = Substitute.For<ICacheService>();
        _tracerProvider = TracerProvider.Default;
        _options = Options.Create(new ExchangeRateProviderSettings { BaseUrl = "https://api.frankfurter.app" });

        _provider = new FrankfurterExchangeRateProvider(_httpClient, _logger, _cacheService, _tracerProvider, _options);
    }


    // Fetch from cache if available
    [Fact]
    public async Task GetLatestRatesAsync_ShouldReturnCachedData_IfAvailable()
    {
        var baseCurrency = "USD";
        var targetCurrency = "EUR";
        var cacheKey = $"ExchangeRates_{baseCurrency}_{targetCurrency}";

        var cachedResponse = new ExchangeRateResponse
        {
            BaseCurrency = baseCurrency,
            Rates = new Dictionary<string, decimal> { { targetCurrency, 0.85m } }
        };

        _cacheService.GetAsync<ExchangeRateResponse>(cacheKey).Returns(Task.FromResult(cachedResponse));

        var result = await _provider.GetLatestRatesAsync(baseCurrency, targetCurrency);

        result.Should().BeEquivalentTo(cachedResponse);
        await _cacheService.DidNotReceive().SetAsync(Arg.Any<string>(), Arg.Any<ExchangeRateResponse>(), Arg.Any<TimeSpan>());
    }

    // Fetch from API if cache is empty
    [Fact]
    public async Task GetLatestRatesAsync_ShouldFetchData_WhenCacheMisses()
    {
        var baseCurrency = "USD";
        var targetCurrency = "EUR";
        var cacheKey = $"ExchangeRates_{baseCurrency}_{targetCurrency}";

        _cacheService.GetAsync<ExchangeRateResponse>(cacheKey).Returns(Task.FromResult<ExchangeRateResponse>(null));

        var responseContent = JsonSerializer.Serialize(new ExchangeRateResponse
        {
            BaseCurrency = baseCurrency,
            Rates = new Dictionary<string, decimal> { { targetCurrency, 0.85m } }
        });

        _fakeHandler.SetResponse((request, cancellationToken) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseContent)
        }));

        var result = await _provider.GetLatestRatesAsync(baseCurrency, targetCurrency);

        result.BaseCurrency.Should().Be(baseCurrency);
        result.Rates.Should().ContainKey(targetCurrency);
        await _cacheService.Received(1).SetAsync(cacheKey, Arg.Any<ExchangeRateResponse>(), Arg.Any<TimeSpan>());
    }

    // HTTP Failure
    [Fact]
    public async Task GetLatestRatesAsync_ShouldThrowException_OnHttpFailure()
    {
        _fakeHandler.SetResponse((request, cancellationToken) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest)));

        Func<Task> act = async () => await _provider.GetLatestRatesAsync("USD", "EUR");

        await act.Should().ThrowAsync<HttpRequestException>().WithMessage("*Failed to retrieve exchange rates*");
    }

    // Circuit Breaker Open
    [Fact]
    public async Task GetLatestRatesAsync_ShouldThrowBrokenCircuitException_WhenCircuitBreakerIsOpen()
    {
        _fakeHandler.SetResponse((request, cancellationToken) => throw new BrokenCircuitException());

        Func<Task> act = async () => await _provider.GetLatestRatesAsync("USD", "EUR");

        await act.Should().ThrowAsync<BrokenCircuitException>();
    }

    // Retry: Success after transient failures
    [Fact]
    public async Task GetLatestRatesAsync_ShouldReturnExchangeRates_AfterSuccessfulRetry()
    {
        var baseCurrency = "USD";
        var targetCurrency = "EUR";
        int attemptCount = 0;

        _fakeHandler.SetResponse(async (request, cancellationToken) =>
        {
            attemptCount++;
            if (attemptCount < 3)
            {
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }

            var responseContent = JsonSerializer.Serialize(new ExchangeRateResponse
            {
                BaseCurrency = baseCurrency,
                Rates = new Dictionary<string, decimal> { { targetCurrency, 0.85m } }
            });

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseContent)
            };
        });

        var result = await _provider.GetLatestRatesAsync(baseCurrency, targetCurrency);

        result.BaseCurrency.Should().Be(baseCurrency);
        result.Rates.Should().ContainKey(targetCurrency);
        result.Rates[targetCurrency].Should().Be(0.85m);
        attemptCount.Should().Be(3);
    }

    [Fact]
    public async Task GetLatestRatesAsync_ShouldThrowBrokenCircuitException_AfterMaxRetries_AndLogWarnings()
    {
        // Arrange
        var baseCurrency = "USD";
        var targetCurrency = "EUR";

        // Simulate failure responses (503 Service Unavailable) to trigger retries and circuit breaker
        _fakeHandler.SetResponse((request, cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)));

        _logger.ClearReceivedCalls(); // Clear previous logs

        Func<Task> act = async () => await _provider.GetLatestRatesAsync(baseCurrency, targetCurrency);

        // Act & Assert
        await act.Should().ThrowAsync<BrokenCircuitException>(); // Expect circuit breaker to be triggered

        // Check that retries were attempted before circuit breaker triggered
        _logger.Received().Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString().Contains("Retry 1 due to ServiceUnavailable")),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception, string>>());

        _logger.Received().Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString().Contains("Retry 2 due to ServiceUnavailable")),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception, string>>());

        _logger.Received().Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString().Contains("Retry 3 due to ServiceUnavailable")),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception, string>>());

        // Ensure circuit breaker warning was logged
        _logger.Received().Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString().Contains("Circuit breaker is open. Returning fallback response.")),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception, string>>());

    }


    [Fact]
    public async Task GetHistoricalRatesAsync_ShouldReturnRates_WhenApiResponseIsValid()
    {
        // Arrange
        var startDate = new DateTime(2024, 3, 1);
        var endDate = new DateTime(2024, 3, 2);
        var baseCurrency = "USD";

        var jsonResponse = """
        {
            "rates": {
                "2024-03-01": { "EUR": 0.92 },
                "2024-03-02": { "EUR": 0.91 }
            }
        }
        """;

        _fakeHandler.SetResponse((req, ct) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(jsonResponse)
        }));

        // Act
        var result = await _provider.GetHistoricalRatesAsync(startDate, endDate, baseCurrency);

        // Assert
        result.Should().NotBeNull();
        result.Rates.Should().ContainKeys(new DateTime(2024, 3, 1), new DateTime(2024, 3, 2));
        result.Rates[DateTime.Parse("2024-03-01")]["EUR"].Should().Be(0.92M);
    }

    [Fact]
    public async Task GetHistoricalRatesAsync_ShouldThrowHttpRequestException_WhenApiReturnsError()
    {
        // Arrange
        var startDate = new DateTime(2024, 3, 1);
        var endDate = new DateTime(2024, 3, 2);
        var baseCurrency = "USD";

        _fakeHandler.SetResponse((req, ct) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest)));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<HttpRequestException>(() => _provider.GetHistoricalRatesAsync(startDate, endDate, baseCurrency));
        exception.Message.Should().Contain("Failed to retrieve historical exchange rates");
    }

    [Fact]
    public async Task GetHistoricalRatesAsync_ShouldThrowKeyNotFoundException_WhenResponseIsEmpty()
    {
        // Arrange
        var startDate = new DateTime(2024, 3, 1);
        var endDate = new DateTime(2024, 3, 2);
        var baseCurrency = "USD";

        _fakeHandler.SetResponse((req, ct) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{}") // Empty JSON
        }));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() => _provider.GetHistoricalRatesAsync(startDate, endDate, baseCurrency));
        exception.Message.Should().Be($"No historical exchange rates found for {baseCurrency}.");
    }

    [Fact]
    public async Task GetHistoricalRatesAsync_ShouldThrowInvalidOperationException_WhenResponseIsInvalidJson()
    {
        // Arrange
        var startDate = new DateTime(2024, 3, 1);
        var endDate = new DateTime(2024, 3, 2);
        var baseCurrency = "USD";

        _fakeHandler.SetResponse((req, ct) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("INVALID_JSON") // Corrupt JSON
        }));

        // Act & Assert
        await FluentActions
            .Awaiting(() => _provider.GetHistoricalRatesAsync(startDate, endDate, baseCurrency))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("The response from the exchange rate provider was not in the expected format.");
    }
}