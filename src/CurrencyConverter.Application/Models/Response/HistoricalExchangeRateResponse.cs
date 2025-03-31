using System.Text.Json.Serialization;

namespace CurrencyConverter.Application.Models.Response;

public class HistoricalExchangeRateResponse
{
    [JsonPropertyName("amount")]
    public decimal Amount { get; set; }

    [JsonPropertyName("base")]
    public string BaseCurrency { get; set; }

    [JsonPropertyName("start_date")]
    public DateTime StartDate { get; set; }

    [JsonPropertyName("end_date")]
    public DateTime EndDate { get; set; }

    [JsonPropertyName("rates")]
    public Dictionary<DateTime, Dictionary<string, decimal>> Rates { get; set; }

    // Pagination Fields
    public int TotalRecords { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}
