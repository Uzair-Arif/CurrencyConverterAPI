using CurrencyConverter.Application.Models.Response;

namespace CurrencyConverter.Application.Interfaces
{
    public interface IExchangeRateProvider
    {
        Task<ExchangeRateResponse> GetLatestRatesAsync(string baseCurrency, string? targetCurrency);
        Task<HistoricalExchangeRateResponse> GetHistoricalRatesAsync(DateTime startDate, DateTime endDate, string baseCurrency);
        string ProviderName { get; }
    }
}