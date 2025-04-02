namespace CurrencyConverter.Application.Interfaces;

public interface IExchangeRateProviderFactory
{
    /// <summary>
    /// Retrieves an exchange rate provider by its name.
    /// </summary>
    /// <param name="providerName">The name of the provider.</param>
    /// <returns>An instance of <see cref="IExchangeRateProvider"/>.</returns>
    IExchangeRateProvider GetProvider(string providerName);
}
