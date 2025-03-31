using Microsoft.Extensions.Logging;
using CurrencyConverter.Application.Interfaces;
using CurrencyConverter.Application.Models.Response;

namespace CurrencyConverter.Application.Services
{
    public class CurrencyService : ICurrencyService
    {
        private readonly IExchangeRateProviderFactory _providerFactory;
        private readonly ILogger<CurrencyService> _logger;
        private static readonly HashSet<string> ExcludedCurrencies = new() { "TRY", "PLN", "THB", "MXN" };

        public CurrencyService(IExchangeRateProviderFactory providerFactory, ILogger<CurrencyService> logger)
        {
            _providerFactory = providerFactory;
            _logger = logger;
        }

        public async Task<ExchangeRateResponse> GetLatestRatesAsync(string baseCurrency, string? targetCurrency = null, string provider = "FrankfurterAPI")
        {
            _logger.LogInformation("Fetching exchange rates for {BaseCurrency} from {Provider}", baseCurrency, provider);

            try
            {
                var exchangeRateProvider = _providerFactory.GetProvider(provider);
                var rates = await exchangeRateProvider.GetLatestRatesAsync(baseCurrency, targetCurrency);

                if (rates == null || rates.Rates.Count == 0)
                {
                    throw new KeyNotFoundException($"No exchange rates found for {baseCurrency}.");
                }

                return rates;
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Exchange rates not found for {BaseCurrency}", baseCurrency);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error fetching exchange rates for {BaseCurrency}", baseCurrency);
                throw new InvalidOperationException("An error occurred while fetching exchange rates. Please try again later.", ex);
            }
        }

        public async Task<CurrencyConversionResponse> ConvertCurrencyAsync(string from, string to, decimal amount, string provider)
        {
            if (ExcludedCurrencies.Contains(from) || ExcludedCurrencies.Contains(to))
            {
                _logger.LogWarning("Conversion involving {From} or {To} is not allowed.", from, to);
                throw new ArgumentException($"Conversion involving {from} or {to} is not allowed.");
            }

            _logger.LogInformation("Fetching exchange rate for {From} to {To} using {Provider}", from, to, provider);

            try
            {
                var latestRates = await GetLatestRatesAsync(from, to, provider);

                if (!latestRates.Rates.TryGetValue(to, out decimal exchangeRate))
                {
                    _logger.LogWarning("Exchange rate not found for conversion from {From} to {To}", from, to);
                    throw new KeyNotFoundException($"Exchange rate not found for conversion from '{from}' to '{to}'.");
                }

                return new CurrencyConversionResponse
                {
                    From = from,
                    To = to,
                    Amount = amount,
                    ConvertedAmount = amount * exchangeRate
                };
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Failed to fetch exchange rates for {From} to {To}", from, to);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error converting currency from {From} to {To}", from, to);
                throw new InvalidOperationException("An error occurred while converting currency. Please try again later.", ex);
            }
        }

        public async Task<HistoricalExchangeRateResponse> GetHistoricalRatesAsync(DateTime startDate, DateTime endDate, string baseCurrency, int page, int pageSize, string provider = "FrankfurterAPI")
        {
            _logger.LogInformation("Fetching historical exchange rates for {BaseCurrency} from {StartDate} to {EndDate}", baseCurrency, startDate, endDate);

            try
            {
                var exchangeRateProvider = _providerFactory.GetProvider(provider);
                var allRates = await exchangeRateProvider.GetHistoricalRatesAsync(startDate, endDate, baseCurrency);

                if (allRates is null || allRates.Rates is null || !allRates.Rates.Any())
                {
                    throw new KeyNotFoundException("No historical exchange rates found.");
                }

                var ratesList = allRates.Rates.ToList();

                int totalRecords = ratesList.Count;
                int totalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);

                // Ensure the page number is within bounds
                if (page < 1) page = 1;
                if (page > totalPages) page = totalPages;

                // Paginate Data
                var paginatedRates = ratesList
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToDictionary(k => k.Key, v => v.Value);

                return new HistoricalExchangeRateResponse
                {
                    BaseCurrency = baseCurrency,
                    StartDate = startDate,
                    EndDate = endDate,
                    Rates = paginatedRates,
                    Amount = allRates.Amount,
                    Page = page,
                    PageSize = pageSize,
                    TotalRecords = totalRecords
                };
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "No historical exchange rates found for {BaseCurrency}", baseCurrency);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error fetching historical exchange rates for {BaseCurrency}", baseCurrency);
                throw new InvalidOperationException("An error occurred while fetching historical exchange rates. Please try again later.", ex);
            }
        }
    }
}