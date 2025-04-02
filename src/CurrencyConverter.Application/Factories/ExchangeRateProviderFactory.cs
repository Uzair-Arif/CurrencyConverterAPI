using CurrencyConverter.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace CurrencyConverter.Application.Factories;

public class ExchangeRateProviderFactory : IExchangeRateProviderFactory
{
    private readonly Dictionary<string, IExchangeRateProvider> _providers;
    private readonly ILogger<ExchangeRateProviderFactory> _logger;

    public ExchangeRateProviderFactory(IEnumerable<IExchangeRateProvider> providers, ILogger<ExchangeRateProviderFactory> logger)
    {
        _providers = providers.ToDictionary(p => p.ProviderName, StringComparer.OrdinalIgnoreCase);
        _logger = logger;
    }

    public IExchangeRateProvider GetProvider(string providerName)
    {
        if (_providers.TryGetValue(providerName, out var provider))
        {
            _logger.LogInformation("Using {ProviderName} for exchange rates.", providerName);
            return provider;
        }

        throw new ArgumentException($"No provider found for {providerName}");
    }
}
