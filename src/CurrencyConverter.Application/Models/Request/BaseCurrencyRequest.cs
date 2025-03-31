namespace CurrencyConverter.Application.Models.Request;

public abstract class BaseCurrencyRequest
{
    public virtual string BaseCurrency { get; set; } = "EUR";
    public string Provider { get; set; } = "FrankfurterAPI"; // Default provider
}
