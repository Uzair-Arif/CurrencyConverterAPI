using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;
using CurrencyConverter.Application.Interfaces;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly bool _enableAuth;

    public IExchangeRateProviderFactory ProviderFactoryMock { get; } = Substitute.For<IExchangeRateProviderFactory>();
    public IExchangeRateProvider FrankfurtProviderMock { get; } = Substitute.For<IExchangeRateProvider>();
    public ICurrencyService CurrencyServiceMock { get; } = Substitute.For<ICurrencyService>();

    public CustomWebApplicationFactory() : this(true) { }
    public CustomWebApplicationFactory(bool enableAuth = true)
    {
        _enableAuth = enableAuth;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IExchangeRateProvider>();
            services.RemoveAll<IExchangeRateProviderFactory>();
            services.RemoveAll<ICurrencyService>();

            ProviderFactoryMock.GetProvider("FrankfurterAPI").Returns(FrankfurtProviderMock);
            services.AddSingleton(ProviderFactoryMock);
            services.AddSingleton(FrankfurtProviderMock);
            services.AddSingleton(CurrencyServiceMock);

            var providerList = new List<IExchangeRateProvider> { FrankfurtProviderMock };
            services.AddSingleton<IEnumerable<IExchangeRateProvider>>(providerList);

            if (_enableAuth)
            {
                services.AddAuthentication("Test")
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", null);
            }
            else
            {
                // Instead of removing, replace with a mock that prevents errors
                services.RemoveAll<IAuthenticationSchemeProvider>();
                services.AddSingleton<IAuthenticationSchemeProvider, TestAuthenticationSchemeProvider>();
            }
        });
    }
}