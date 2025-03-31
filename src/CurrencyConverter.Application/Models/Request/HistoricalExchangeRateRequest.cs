namespace CurrencyConverter.Application.Models.Request;

public class HistoricalExchangeRateRequest : BaseCurrencyRequest
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
