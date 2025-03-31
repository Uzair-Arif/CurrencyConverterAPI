using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using Polly.Extensions.Http;
using Polly.Retry;
using OpenTelemetry.Trace;
using CurrencyConverter.Application.Interfaces;
using CurrencyConverter.Application.Models.Response;
using Microsoft.Extensions.Options;
using CurrencyConverter.Application.Models.Configuration;

namespace CurrencyConverter.Infrastructure.Providers
{
    public class FrankfurterExchangeRateProvider : IExchangeRateProvider
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<FrankfurterExchangeRateProvider> _logger;
        private readonly ICacheService _cacheService;
        private readonly AsyncRetryPolicy<HttpResponseMessage> _retryPolicy;
        private readonly AsyncCircuitBreakerPolicy<HttpResponseMessage> _circuitBreakerPolicy;
        private readonly Tracer _tracer;
        private readonly string _baseUrl;

        public string ProviderName => "FrankfurterAPI";
        private const string CacheKeyPrefix = "ExchangeRates_";
        private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(10);

        public FrankfurterExchangeRateProvider(HttpClient httpClient,
                                               ILogger<FrankfurterExchangeRateProvider> logger,
                                               ICacheService cacheService,
                                               TracerProvider traceProvider,
                                               IOptions<ExchangeRateProviderSettings> options)
        {
            _httpClient = httpClient;
            _logger = logger;
            _cacheService = cacheService;
            _tracer = traceProvider.GetTracer("CurrencyConverterAPI");
            _baseUrl = options.Value.BaseUrl.TrimEnd('/');

            _retryPolicy = HttpPolicyExtensions
                .HandleTransientHttpError()
                .WaitAndRetryAsync(5, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    (result, timeSpan, retryCount, context) =>
                    {
                        _logger.LogWarning("Retry {RetryCount} due to {Reason}", retryCount, result.Result?.StatusCode);
                    });

            _circuitBreakerPolicy = HttpPolicyExtensions
                .HandleTransientHttpError()
                .CircuitBreakerAsync(3, TimeSpan.FromSeconds(30),
                    (exception, timespan) =>
                    {
                        _logger.LogWarning("Circuit Breaker triggered! Breaking for {Timespan}", timespan);
                    },
                    () =>
                    {
                        _logger.LogInformation("Circuit Breaker reset.");
                    });
        }

        public async Task<ExchangeRateResponse> GetLatestRatesAsync(string baseCurrency, string? targetCurrency)
        {
            string cacheKey = targetCurrency is not null
                ? $"{CacheKeyPrefix}{baseCurrency}_{targetCurrency}"
                : $"{CacheKeyPrefix}{baseCurrency}";

            var cachedRates = await _cacheService.GetAsync<ExchangeRateResponse>(cacheKey);
            if (cachedRates is not null)
            {
                _logger.LogInformation("Returning cached exchange rates for {BaseCurrency} {TargetCurrency}", baseCurrency, targetCurrency ?? "ALL");
                return cachedRates;
            }

            var url = targetCurrency is not null
                ? $"{_baseUrl}/latest?from={baseCurrency}&symbols={targetCurrency}"
                : $"{_baseUrl}/latest?from={baseCurrency}";

            using var activity = _tracer.StartActiveSpan("FrankfurterAPI_GetLatestRates");
            activity.SetAttribute("http.url", url);
            activity.SetAttribute("currency.base", baseCurrency);
            if (targetCurrency is not null)
            {
                activity.SetAttribute("currency.target", targetCurrency);
            }

            _logger.LogInformation("Fetching exchange rates from Frankfurter API for {BaseCurrency} {TargetCurrency}", baseCurrency, targetCurrency ?? "ALL");

            var stopwatch = Stopwatch.StartNew();

            try
            {
                var response = await _retryPolicy.WrapAsync(_circuitBreakerPolicy).ExecuteAsync(() => _httpClient.GetAsync(url));
                stopwatch.Stop();

                activity.SetAttribute("http.status_code", (int)response.StatusCode);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Failed to retrieve exchange rates. Status: {StatusCode}, URL: {RequestUrl}", response.StatusCode, url);
                    throw new HttpRequestException($"Failed to retrieve exchange rates. Status: {response.StatusCode}");
                }

                var content = await response.Content.ReadAsStringAsync();

                var exchangeRates = JsonSerializer.Deserialize<ExchangeRateResponse>(content)
                    ?? throw new InvalidOperationException("Invalid response received from exchange rate provider.");

                await _cacheService.SetAsync(cacheKey, exchangeRates, CacheDuration);
                _logger.LogInformation("Cached exchange rates for {BaseCurrency} {TargetCurrency} for {CacheDuration} minutes",
                    baseCurrency, targetCurrency ?? "ALL", CacheDuration.TotalMinutes);

                _logger.LogInformation("Exchange rates fetched in {ElapsedMilliseconds} ms", stopwatch.ElapsedMilliseconds);
                activity.SetAttribute("http.response_time_ms", stopwatch.ElapsedMilliseconds);

                return exchangeRates;
            }
            catch (BrokenCircuitException)
            {
                _logger.LogWarning("Circuit breaker is open. Returning fallback response.");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching exchange rates from Frankfurter API. URL: {RequestUrl}", url);
                throw;
            }
        }

        public async Task<HistoricalExchangeRateResponse> GetHistoricalRatesAsync(DateTime startDate, DateTime endDate, string baseCurrency)
        {
            var formattedStartDate = startDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            var formattedEndDate = endDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            var requestUrl = $"{_baseUrl}/{formattedStartDate}..{formattedEndDate}?from={baseCurrency}";

            using var activity = _tracer.StartActiveSpan("FrankfurterAPI_GetHistoricalRates");
            activity.SetAttribute("http.url", requestUrl);
            activity.SetAttribute("currency.base", baseCurrency);
            activity.SetAttribute("date.start", formattedStartDate);
            activity.SetAttribute("date.end", formattedEndDate);

            _logger.LogInformation("Fetching historical exchange rates from {StartDate} to {EndDate} for {BaseCurrency}",
                formattedStartDate, formattedEndDate, baseCurrency);

            var stopwatch = Stopwatch.StartNew();

            try
            {
                var response = await _retryPolicy.WrapAsync(_circuitBreakerPolicy).ExecuteAsync(() => _httpClient.GetAsync(requestUrl));
                stopwatch.Stop();

                activity.SetAttribute("http.status_code", (int)response.StatusCode);
                if (!response.IsSuccessStatusCode)
                {
                    throw new HttpRequestException($"Failed to retrieve historical exchange rates. Status: {response.StatusCode}");
                }

                var content = await response.Content.ReadAsStringAsync();

                HistoricalExchangeRateResponse historicalRates;
                try
                {
                    historicalRates = JsonSerializer.Deserialize<HistoricalExchangeRateResponse>(content)
                        ?? throw new InvalidOperationException("Invalid response received from exchange rate provider.");
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "Invalid JSON received from exchange rate provider. URL: {RequestUrl}, Response: {Content}", requestUrl, content);
                    throw new InvalidOperationException("The response from the exchange rate provider was not in the expected format.", ex);
                }

                if (historicalRates.Rates == null || !historicalRates.Rates.Any())
                {
                    throw new KeyNotFoundException($"No historical exchange rates found for {baseCurrency}.");
                }

                _logger.LogInformation("Fetched historical exchange rates in {ElapsedMilliseconds} ms", stopwatch.ElapsedMilliseconds);
                activity.SetAttribute("http.response_time_ms", stopwatch.ElapsedMilliseconds);

                return historicalRates;
            }

            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching historical exchange rates from Frankfurter API. URL: {RequestUrl}", requestUrl);
                throw;
            }
        }
    }
}