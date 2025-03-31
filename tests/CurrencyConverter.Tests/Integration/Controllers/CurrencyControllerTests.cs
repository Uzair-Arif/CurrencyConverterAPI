using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CurrencyConverter.Application.Interfaces;
using CurrencyConverter.Application.Models.Response;
using FluentAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

public class CurrencyControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly IExchangeRateProvider _frankfurtProvider;

    public CurrencyControllerTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
        _frankfurtProvider = factory.FrankfurtProviderMock; // Use the specific provider mock
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

        // Ensure the mock provider returns the expected response
        _frankfurtProvider.GetLatestRatesAsync("USD", null)
                          .Returns(Task.FromResult(expectedResponse));

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/currency/rates?BaseCurrency=USD&Provider=FrankfurterAPI");
        request.Headers.Authorization = new AuthenticationHeaderValue("Test");

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
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/currency/rates?BaseCurrency=INVALID");
        request.Headers.Authorization = new AuthenticationHeaderValue("Test");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var errorMessage = await response.Content.ReadAsStringAsync();
        errorMessage.Should().Contain("Base currency must be a 3-letter code");
    }

    [Fact]
    public async Task GetRates_ShouldReturn401_WhenUnauthorized()
    {
        // Use a factory instance where authentication is disabled
        var factoryWithoutAuth = new CustomWebApplicationFactory(enableAuth: false);
        var clientWithoutAuth = factoryWithoutAuth.CreateClient();

        // Act
        var response = await clientWithoutAuth.GetAsync("/api/currency/rates?BaseCurrency=USD");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetRates_ShouldReturn403_WhenUserLacksPermission()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/currency/rates?BaseCurrency=USD");
        request.Headers.Authorization = new AuthenticationHeaderValue("Test", "Bearer mock-token-for-user-without-access");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetRates_ShouldReturn429_WhenRateLimitExceeded()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/currency/rates?BaseCurrency=USD");
        request.Headers.Authorization = new AuthenticationHeaderValue("Test");

        for (int i = 0; i < 10; i++) // Simulate 10 rapid requests
        {
            await _client.SendAsync(request);
        }

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task GetRates_ShouldReturn500_WhenProviderFails()
    {
        // Arrange
        _frankfurtProvider.GetLatestRatesAsync(Arg.Any<string>(), Arg.Any<string>())
                             .Throws(new Exception("Provider failure"));

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/currency/rates?BaseCurrency=USD");
        request.Headers.Authorization = new AuthenticationHeaderValue("Test");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        var errorMessage = await response.Content.ReadAsStringAsync();
        errorMessage.Should().Contain("An error occurred while fetching exchange rates");
    }
}