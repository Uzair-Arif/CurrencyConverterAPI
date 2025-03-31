namespace CurrencyConverter.Application.Models.Request;

public class ConvertCurrencyRequest : BaseCurrencyRequest
{
    public string From { get; set; } = "EUR"; // Default from currency
    public string To { get; set; } = "USD"; // Default to currency
    public decimal Amount { get; set; } = 1m; // Default to 1
}