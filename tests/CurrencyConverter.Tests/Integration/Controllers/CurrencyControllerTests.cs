using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CurrencyConverter.Application.Interfaces;
using CurrencyConverter.Application.Models.Request;
using CurrencyConverter.Application.Models.Response;
using FluentAssertions;
using NSubstitute;
using NSubstitute.ClearExtensions;
using NSubstitute.ExceptionExtensions;

[Collection("CurrencyApiAuthTests")]
public class CurrencyControllerTests : IClassFixture<CustomWebApplicationFactoryWithAuth>
{
    private readonly HttpClient _client;
    private readonly IExchangeRateProvider _frankfurtProvider;
    private readonly ICurrencyService _currencyService;

    public CurrencyControllerTests(CustomWebApplicationFactoryWithAuth factory)
    {
        _client = factory.CreateClient();
        _frankfurtProvider = factory.FrankfurtProviderMock;
        _currencyService = factory.CurrencyServiceMock;

        // Reset the mock to ensure test independence
        _frankfurtProvider.ClearSubstitute();
        _currencyService.ClearSubstitute();
    }

    [Fact]
    public async Task GetRates_ShouldReturn200_WhenValidRequestForFrankfurt()
    {
        // Arrange
        var expectedResponse = new ExchangeRateResponse
        {
            BaseCurrency = "USD",
            Rates = new Dictionary<string, decimal> { { "EUR", 0.85m } }
        };

        _currencyService.GetLatestRatesAsync("USD", provider: "FrankfurterAPI")
                        .Returns(Task.FromResult(expectedResponse));

        // Ensure the mock provider returns the expected response
        _frankfurtProvider.GetLatestRatesAsync("USD", null)
                          .Returns(Task.FromResult(expectedResponse));


        var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/currency/rates?BaseCurrency=USD&Provider=FrankfurterAPI");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "mock-token-for-admin");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ExchangeRateResponse>();
        result.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task GetRates_ShouldReturn400_WhenInvalidBaseCurrency()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/currency/rates?BaseCurrency=INVALID");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "mock-token-for-admin");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var errorMessage = await response.Content.ReadAsStringAsync();
        errorMessage.Should().Contain("Base currency must be a 3-letter code");
    }

    [Fact]
    public async Task GetRates_ShouldReturn403_WhenUserLacksPermission()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/currency/rates?BaseCurrency=USD");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "mock-token-for-user"); // <-- Use a valid token

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetRates_ShouldReturn429_WhenRateLimitExceeded()
    {
        // Arrange
        var url = "/api/v1/currency/rates?BaseCurrency=USD";
        var authHeader = new AuthenticationHeaderValue("Bearer", "mock-token-for-admin");

        for (int i = 0; i < 10; i++) // Simulate 10 rapid requests
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = authHeader;
            await _client.SendAsync(request);
        }

        // Act
        var finalRequest = new HttpRequestMessage(HttpMethod.Get, url);
        finalRequest.Headers.Authorization = authHeader;
        var response = await _client.SendAsync(finalRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task GetRates_ShouldReturn500_WhenProviderFails()
    {
        // Arrange
        _currencyService.GetLatestRatesAsync("USD", provider: "FrankfurterAPI")
                       .Throws(new Exception("Provider failure"));

        _frankfurtProvider.GetLatestRatesAsync(Arg.Any<string>(), Arg.Any<string>())
                             .Throws(new Exception("Provider failure"));

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/currency/rates?BaseCurrency=USD");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "mock-token-for-admin");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        var errorMessage = await response.Content.ReadAsStringAsync();
        errorMessage.Should().Contain("An error occurred while fetching exchange rates");
    }

    // ConvertCurrency Tests

    [Fact]
    public async Task ConvertCurrency_ShouldReturn200_WhenValidRequest()
    {
        // Arrange
        var request = new ConvertCurrencyRequest { From = "USD", To = "EUR", Amount = 100, Provider = "FrankfurterAPI" };
        var expectedResponse = new CurrencyConversionResponse { From = "USD", To = "EUR", Amount = 100, ConvertedAmount = 85 };

        _currencyService.ConvertCurrencyAsync("USD", "EUR", 100, "FrankfurterAPI")
                        .Returns(Task.FromResult(expectedResponse));

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/currency/convert")
        {
            Content = JsonContent.Create(request),
            Headers = { Authorization = new AuthenticationHeaderValue("Bearer", "mock-token-for-admin") }
        };

        // Act
        var response = await _client.SendAsync(httpRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<CurrencyConversionResponse>();
        result.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task ConvertCurrency_ShouldReturn400_WhenInvalidRequest()
    {
        // Arrange (Invalid request: missing required fields)
        var request = new ConvertCurrencyRequest { From = "", To = "EUR", Amount = -10, Provider = "" };

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/currency/convert")
        {
            Content = JsonContent.Create(request),
            Headers = { Authorization = new AuthenticationHeaderValue("Bearer", "mock-token-for-admin") }
        };

        // Act
        var response = await _client.SendAsync(httpRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // GetHistoricalRates Tests

    [Fact]
    public async Task GetHistoricalRates_ShouldReturn200_WhenValidRequest()
    {
        // Arrange
        var request = new HistoricalExchangeRateRequest
        {
            StartDate = DateTime.UtcNow.AddDays(-7),
            EndDate = DateTime.UtcNow,
            BaseCurrency = "USD",
            Page = 1,
            PageSize = 10,
            Provider = "FrankfurterAPI"
        };

        var expectedResponse = new HistoricalExchangeRateResponse
        {
            Rates = new Dictionary<DateTime, Dictionary<string, decimal>>
            {
                { DateTime.UtcNow.AddDays(-3), new Dictionary<string, decimal> { { "EUR", 0.85m } } }
            }
        };

        _currencyService.GetHistoricalRatesAsync(request.StartDate, request.EndDate, request.BaseCurrency, request.Page, request.PageSize, request.Provider)
                        .Returns(Task.FromResult(expectedResponse));

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/currency/historical")
        {
            Content = JsonContent.Create(request),
            Headers = { Authorization = new AuthenticationHeaderValue("Bearer", "mock-token-for-admin") }
        };

        // Act
        var response = await _client.SendAsync(httpRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<HistoricalExchangeRateResponse>();
        result.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task GetHistoricalRates_ShouldReturn400_WhenInvalidRequest()
    {
        // Arrange (Invalid request: missing base currency)
        var request = new HistoricalExchangeRateRequest
        {
            StartDate = DateTime.UtcNow.AddDays(-7),
            EndDate = DateTime.UtcNow,
            BaseCurrency = "",
            Page = 1,
            PageSize = 10,
            Provider = "FrankfurterAPI"
        };

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/currency/historical")
        {
            Content = JsonContent.Create(request),
            Headers = { Authorization = new AuthenticationHeaderValue("Bearer", "mock-token-for-admin") }
        };

        // Act
        var response = await _client.SendAsync(httpRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetHistoricalRates_ShouldReturn404_WhenNoDataFound()
    {
        // Arrange
        var request = new HistoricalExchangeRateRequest
        {
            StartDate = DateTime.UtcNow.AddDays(-30),
            EndDate = DateTime.UtcNow,
            BaseCurrency = "USD",
            Page = 1,
            PageSize = 10,
            Provider = "FrankfurterAPI"
        };

        _currencyService.GetHistoricalRatesAsync(request.StartDate, request.EndDate, request.BaseCurrency, request.Page, request.PageSize, request.Provider)
                        .Returns(Task.FromResult<HistoricalExchangeRateResponse>(null));

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/currency/historical")
        {
            Content = JsonContent.Create(request),
            Headers = { Authorization = new AuthenticationHeaderValue("Bearer", "mock-token-for-admin") }
        };

        // Act
        var response = await _client.SendAsync(httpRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

}

[CollectionDefinition("CurrencyApiNoAuthTests")]
public class CurrencyControllerUnauthorizedTests : IClassFixture<CustomWebApplicationFactoryWithoutAuth>
{
    private readonly HttpClient _client;

    public CurrencyControllerUnauthorizedTests(CustomWebApplicationFactoryWithoutAuth factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetRates_ShouldReturn401_WhenUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/currency/rates?BaseCurrency=USD");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}