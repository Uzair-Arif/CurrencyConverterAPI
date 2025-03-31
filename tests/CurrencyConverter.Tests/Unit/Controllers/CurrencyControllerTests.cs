using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using CurrencyConverter.API.Controllers;
using CurrencyConverter.Application.Interfaces;
using CurrencyConverter.Application.Models.Request;
using CurrencyConverter.Application.Models.Response;
using NSubstitute.ExceptionExtensions;

namespace CurrencyConverter.Tests.Controllers
{
    public class CurrencyControllerTests
    {
        private readonly ICurrencyService _currencyService = Substitute.For<ICurrencyService>();
        private readonly IValidator<ConvertCurrencyRequest> _convertCurrencyValidator = Substitute.For<IValidator<ConvertCurrencyRequest>>();
        private readonly IValidator<HistoricalExchangeRateRequest> _historicalRatesValidator = Substitute.For<IValidator<HistoricalExchangeRateRequest>>();
        private readonly IValidator<GetRatesRequest> _getRatesValidator = Substitute.For<IValidator<GetRatesRequest>>();

        private readonly CurrencyController _controller;

        public CurrencyControllerTests()
        {
            _controller = new CurrencyController(
                _currencyService,
                _convertCurrencyValidator,
                _historicalRatesValidator,
                _getRatesValidator
            );
        }

        [Fact]
        public async Task GetRates_ValidRequest_ReturnsOk()
        {
            // Arrange
            var request = new GetRatesRequest { BaseCurrency = "USD" };
            _getRatesValidator.ValidateAsync(request).Returns(new ValidationResult());

            var response = new ExchangeRateResponse { BaseCurrency = "USD", Rates = new Dictionary<string, decimal> { { "EUR", 0.85m } } };
            _currencyService
                .GetLatestRatesAsync(baseCurrency: "USD", targetCurrency: null, provider: "FrankfurterAPI")
                .Returns(response);

            // Act
            var result = await _controller.GetRates(request);

            // Assert
            result.Should().BeOfType<OkObjectResult>()
                .Which.Value.Should().BeEquivalentTo(response);
        }

        [Fact]
        public async Task GetRates_InvalidRequest_ReturnsBadRequest()
        {
            // Arrange
            var request = new GetRatesRequest(); // Missing required fields
            var validationErrors = new ValidationResult(new List<ValidationFailure>
            {
                new ValidationFailure("BaseCurrency", "BaseCurrency is required.")
            });
            _getRatesValidator.ValidateAsync(request).Returns(validationErrors);

            // Act
            var result = await _controller.GetRates(request);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>()
                .Which.Value.Should().BeEquivalentTo(validationErrors.Errors);
        }

        [Fact]
        public async Task GetRates_CurrencyNotFound_ReturnsNotFound()
        {
            // Arrange
            var request = new GetRatesRequest { BaseCurrency = "XYZ" };
            _getRatesValidator.ValidateAsync(request).Returns(new ValidationResult());

            _currencyService.GetLatestRatesAsync(baseCurrency: request.BaseCurrency, provider: request.Provider)
                .Throws(new KeyNotFoundException("Currency not found"));

            // Act
            var result = await _controller.GetRates(request);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>()
                .Which.Value.Should().BeEquivalentTo(new
                {
                    message = "Currency rates not found.",
                    error = "Currency not found"
                });
        }

        [Fact]
        public async Task GetRates_ServiceThrowsException_ReturnsInternalServerError()
        {
            // Arrange
            var request = new GetRatesRequest { BaseCurrency = "USD" };
            _getRatesValidator.ValidateAsync(request).Returns(new ValidationResult());

            _currencyService.GetLatestRatesAsync(baseCurrency: request.BaseCurrency, provider: request.Provider)
                .Throws(new Exception("Unexpected error in GetRates"));

            // Act
            var result = await _controller.GetRates(request);

            // Assert
            result.Should().BeOfType<ObjectResult>()
                .Which.StatusCode.Should().Be(500);

            var objectResult = result as ObjectResult;
            objectResult!.Value.Should().BeEquivalentTo(new
            {
                message = "An error occurred while fetching exchange rates.",
                error = "Unexpected error in GetRates"
            });
        }

        [Fact]
        public async Task GetRates_ShouldReturnBadRequest_WhenArgumentExceptionThrown()
        {
            // Arrange
            var request = new GetRatesRequest { BaseCurrency = "INVALID" };

            // Mock validation to pass
            _getRatesValidator
                .ValidateAsync(request, default)
                .Returns(new ValidationResult());

            // Mock service to throw ArgumentException
            _currencyService
                .GetLatestRatesAsync(request.BaseCurrency, null, request.Provider)
                .Throws(new ArgumentException("Invalid currency code."));

            // Act
            var result = await _controller.GetRates(request);

            // Assert
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            badRequestResult.Value.Should().BeEquivalentTo(new
            {
                message = "Invalid request parameters.",
                error = "Invalid currency code."
            });
        }

        [Fact]
        public async Task ConvertCurrency_ValidRequest_ReturnsOk()
        {
            // Arrange
            var request = new ConvertCurrencyRequest { From = "USD", To = "EUR", Amount = 100 };
            _convertCurrencyValidator.ValidateAsync(request).Returns(new ValidationResult());

            var response = new CurrencyConversionResponse { ConvertedAmount = 85, From = "USD", To = "EUR", Amount = 100 };
            _currencyService.ConvertCurrencyAsync(request.From, request.To, request.Amount, request.Provider).Returns(response);

            // Act
            var result = await _controller.ConvertCurrency(request);

            // Assert
            result.Should().BeOfType<OkObjectResult>()
                .Which.Value.Should().BeEquivalentTo(response);
        }

        [Fact]
        public async Task ConvertCurrency_InvalidRequest_ReturnsBadRequest()
        {
            // Arrange
            var request = new ConvertCurrencyRequest { From = "", To = "", Amount = -1 };
            var validationErrors = new ValidationResult(new List<ValidationFailure>
            {
                new ValidationFailure("From", "From currency is required."),
                new ValidationFailure("Amount", "Amount must be greater than zero.")
            });
            _convertCurrencyValidator.ValidateAsync(request).Returns(validationErrors);

            // Act
            var result = await _controller.ConvertCurrency(request);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>()
                .Which.Value.Should().BeEquivalentTo(validationErrors.Errors);
        }

        [Fact]
        public async Task ConvertCurrency_CurrencyNotFound_ReturnsNotFound()
        {
            // Arrange
            var request = new ConvertCurrencyRequest { From = "XYZ", To = "ABC", Amount = 100 };
            _convertCurrencyValidator.ValidateAsync(request).Returns(new ValidationResult());

            _currencyService.ConvertCurrencyAsync(request.From, request.To, request.Amount, request.Provider)
                .Throws(new KeyNotFoundException("Exchange rate not found"));

            // Act
            var result = await _controller.ConvertCurrency(request);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>()
                .Which.Value.Should().BeEquivalentTo(new
                {
                    message = "Currency conversion rates not found.",
                    error = "Exchange rate not found"
                });
        }

        [Fact]
        public async Task ConvertCurrency_ServiceThrowsException_ReturnsInternalServerError()
        {
            // Arrange
            var request = new ConvertCurrencyRequest { From = "USD", To = "EUR", Amount = 100 };
            _convertCurrencyValidator.ValidateAsync(request).Returns(new ValidationResult());

            _currencyService.ConvertCurrencyAsync(request.From, request.To, request.Amount, request.Provider)
                .Throws(new Exception("Unexpected error in ConvertCurrency"));

            // Act
            var result = await _controller.ConvertCurrency(request);

            // Assert
            result.Should().BeOfType<ObjectResult>()
                .Which.StatusCode.Should().Be(500);

            var objectResult = result as ObjectResult;
            objectResult!.Value.Should().BeEquivalentTo(new
            {
                message = "An error occurred while converting currency.",
                error = "Unexpected error in ConvertCurrency"
            });
        }

        [Fact]
        public async Task GetHistoricalRates_ValidRequest_ReturnsOk()
        {
            // Arrange
            var request = new HistoricalExchangeRateRequest { StartDate = DateTime.Now.AddDays(-7), EndDate = DateTime.Now, BaseCurrency = "USD", Page = 1, PageSize = 10 };
            _historicalRatesValidator.ValidateAsync(request).Returns(new ValidationResult());

            var response = new HistoricalExchangeRateResponse
            {
                BaseCurrency = "USD",
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                Rates = new Dictionary<DateTime, Dictionary<string, decimal>>
                {
                    { new DateTime(2024, 01, 01), new Dictionary<string, decimal> { { "EUR", 0.85m } } }
                },
                Page = request.Page,
                PageSize = request.PageSize,
                TotalRecords = 1
            };
            _currencyService.GetHistoricalRatesAsync(request.StartDate, request.EndDate, request.BaseCurrency, request.Page, request.PageSize, request.Provider)
               .Returns(Task.FromResult(response));

            // Act
            var result = await _controller.GetHistoricalRates(request);

            // Assert
            result.Should().BeOfType<OkObjectResult>()
                .Which.Value.Should().BeEquivalentTo(response);
        }

        [Fact]
        public async Task GetHistoricalRates_InvalidRequest_ReturnsBadRequest()
        {
            // Arrange
            var request = new HistoricalExchangeRateRequest(); // Missing required fields
            var validationErrors = new ValidationResult(new List<ValidationFailure>
            {
                new ValidationFailure("StartDate", "StartDate is required.")
            });
            _historicalRatesValidator.ValidateAsync(request).Returns(validationErrors);

            // Act
            var result = await _controller.GetHistoricalRates(request);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>()
                .Which.Value.Should().BeEquivalentTo(validationErrors.Errors);
        }

        [Fact]
        public async Task GetHistoricalRates_NoDataFound_ReturnsNotFound()
        {
            // Arrange
            var request = new HistoricalExchangeRateRequest { StartDate = DateTime.Now.AddDays(-30), EndDate = DateTime.Now, BaseCurrency = "USD" };
            _historicalRatesValidator.ValidateAsync(request).Returns(new ValidationResult());

            _currencyService.GetHistoricalRatesAsync(request.StartDate, request.EndDate, request.BaseCurrency, request.Page, request.PageSize, request.Provider)
              .Throws(new KeyNotFoundException("Historical rates not found."));

            // Act
            var result = await _controller.GetHistoricalRates(request);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>()
            .Which.Value.Should().BeEquivalentTo(new
            {
                message = "No historical data found for the given parameters.",
                error = "Historical rates not found."
            });
        }

        [Fact]
        public async Task GetHistoricalRates_ServiceThrowsException_ReturnsInternalServerError()
        {
            // Arrange
            var request = new HistoricalExchangeRateRequest { StartDate = DateTime.Now.AddDays(-7), EndDate = DateTime.Now, BaseCurrency = "USD" };
            _historicalRatesValidator.ValidateAsync(request).Returns(new ValidationResult());

            _currencyService.GetHistoricalRatesAsync(request.StartDate, request.EndDate, request.BaseCurrency, request.Page, request.PageSize, request.Provider)
                .Throws(new Exception("Unexpected error in GetHistoricalRates"));

            // Act
            var result = await _controller.GetHistoricalRates(request);

            // Assert
            result.Should().BeOfType<ObjectResult>()
                .Which.StatusCode.Should().Be(500);

            var objectResult = result as ObjectResult;
            objectResult!.Value.Should().BeEquivalentTo(new
            {
                message = "An error occurred while fetching historical exchange rates.",
                error = "Unexpected error in GetHistoricalRates"
            });
        }

        [Fact]
        public async Task GetHistoricalRates_ShouldReturnNotFound_WhenResultIsNull()
        {
            // Arrange
            var request = new HistoricalExchangeRateRequest
            {
                BaseCurrency = "USD",
                StartDate = DateTime.UtcNow.AddDays(-10),
                EndDate = DateTime.UtcNow.AddDays(-5)
            };

            // Mock validation to pass
            _historicalRatesValidator
                .ValidateAsync(request, default)
                .Returns(new ValidationResult());

            // Mock service to return null (no historical data found)
            _currencyService
                .GetHistoricalRatesAsync(request.StartDate, request.EndDate, request.BaseCurrency, request.Page, request.PageSize, request.Provider)
                .Returns(Task.FromResult<HistoricalExchangeRateResponse>(null));

            // Act
            var result = await _controller.GetHistoricalRates(request);

            // Assert
            var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
            notFoundResult.Value.Should().Be("Historical rates not found.");
        }
    }
}