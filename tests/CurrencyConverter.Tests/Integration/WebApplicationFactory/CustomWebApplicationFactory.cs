using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using System.Text.Encodings.Web;
using NSubstitute;
using CurrencyConverter.Application.Interfaces;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly bool _enableAuth;

    public IExchangeRateProviderFactory ProviderFactoryMock { get; } = Substitute.For<IExchangeRateProviderFactory>();
    public IExchangeRateProvider FrankfurtProviderMock { get; } = Substitute.For<IExchangeRateProvider>();

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

            
            ProviderFactoryMock.GetProvider("FrankfurterAPI").Returns(FrankfurtProviderMock);

            services.AddSingleton(ProviderFactoryMock);
            services.AddSingleton(FrankfurtProviderMock);

            
            var providerList = new List<IExchangeRateProvider> { FrankfurtProviderMock };
            services.AddSingleton<IEnumerable<IExchangeRateProvider>>(providerList);

            if (_enableAuth)
            {
                
                services.AddAuthentication("Test")
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", null);
            }
            else
            {
                
                services.RemoveAll<IAuthenticationSchemeProvider>();
            }
        });
    }
}

//   **Custom Authentication Handler (Mocks JWT authentication)**
public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public TestAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock)
        : base(options, logger, encoder, clock) { }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new[] { new Claim(ClaimTypes.Name, "TestUser"), new Claim(ClaimTypes.Role, "User") };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "Test");

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}