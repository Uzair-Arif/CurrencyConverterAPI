using CurrencyConverter.Application.Models.Response;

namespace CurrencyConverter.Application.Interfaces;

public interface ICurrencyService
{
    Task<ExchangeRateResponse> GetLatestRatesAsync(string baseCurrency, string? targetCurrency = null, string provider = "FrankfurterAPI");
    Task<CurrencyConversionResponse> ConvertCurrencyAsync(string from, string to, decimal amount, string provider);
    Task<HistoricalExchangeRateResponse> GetHistoricalRatesAsync(
     DateTime startDate, DateTime endDate, string baseCurrency, int page, int pageSize, string provider);
}
