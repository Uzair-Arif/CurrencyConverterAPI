using CurrencyConverter.Application.Factories;
using CurrencyConverter.Application.Interfaces;
using CurrencyConverter.Application.Models.Request;
using CurrencyConverter.Application.Services;
using CurrencyConverter.Application.Services.Security;
using CurrencyConverter.Application.Validators;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace CurrencyConverter.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            // Register application services
            services.AddScoped<ICurrencyService, CurrencyService>();
            services.AddScoped<ITokenService, TokenService>();
            services.AddSingleton<IExchangeRateProviderFactory, ExchangeRateProviderFactory>();

            // Register validators
            services.AddScoped<IValidator<ConvertCurrencyRequest>, ConvertCurrencyRequestValidator>();
            services.AddScoped<IValidator<HistoricalExchangeRateRequest>, HistoricalExchangeRateRequestValidator>();
            services.AddScoped<IValidator<GetRatesRequest>, GetRatesRequestValidator>();

            return services;
        }
    }
}