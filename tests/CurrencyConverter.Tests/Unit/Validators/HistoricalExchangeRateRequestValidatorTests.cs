using CurrencyConverter.Application.Models.Request;
using CurrencyConverter.Application.Validators;
using FluentValidation.TestHelper;

namespace CurrencyConverter.Tests.Application.Validators
{
    public class HistoricalExchangeRateRequestValidatorTests
    {
        private readonly HistoricalExchangeRateRequestValidator _validator;

        public HistoricalExchangeRateRequestValidatorTests()
        {
            _validator = new HistoricalExchangeRateRequestValidator();
        }

        [Fact]
        public void Validate_ShouldFail_WhenStartDateIsEmpty()
        {
            // Arrange
            var request = new HistoricalExchangeRateRequest
            {
                BaseCurrency = "USD",
                StartDate = default,
                EndDate = DateTime.UtcNow,
                Page = 1,
                PageSize = 10
            };

            // Act
            var result = _validator.TestValidate(request);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.StartDate)
                  .WithErrorMessage("Start date is required.");
        }

        [Fact]
        public void Validate_ShouldFail_WhenStartDateIsInFuture()
        {
            // Arrange
            var request = new HistoricalExchangeRateRequest
            {
                BaseCurrency = "USD",
                StartDate = DateTime.UtcNow.AddDays(1),
                EndDate = DateTime.UtcNow,
                Page = 1,
                PageSize = 10
            };

            // Act
            var result = _validator.TestValidate(request);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.StartDate)
                  .WithErrorMessage("Start date cannot be in the future.");
        }

        [Fact]
        public void Validate_ShouldFail_WhenEndDateIsEmpty()
        {
            // Arrange
            var request = new HistoricalExchangeRateRequest
            {
                BaseCurrency = "USD",
                StartDate = DateTime.UtcNow.AddDays(-10),
                EndDate = default,
                Page = 1,
                PageSize = 10
            };

            // Act
            var result = _validator.TestValidate(request);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.EndDate)
                  .WithErrorMessage("End date is required.");
        }

        [Fact]
        public void Validate_ShouldFail_WhenEndDateIsInFuture()
        {
            // Arrange
            var request = new HistoricalExchangeRateRequest
            {
                BaseCurrency = "USD",
                StartDate = DateTime.UtcNow.AddDays(-10),
                EndDate = DateTime.UtcNow.AddDays(1),
                Page = 1,
                PageSize = 10
            };

            // Act
            var result = _validator.TestValidate(request);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.EndDate)
                  .WithErrorMessage("End date cannot be in the future.");
        }

        [Fact]
        public void Validate_ShouldFail_WhenStartDateIsAfterEndDate()
        {
            // Arrange
            var request = new HistoricalExchangeRateRequest
            {
                BaseCurrency = "USD",
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(-1),
                Page = 1,
                PageSize = 10
            };

            // Act
            var result = _validator.TestValidate(request);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.StartDate)
                  .WithErrorMessage("Start date must be before or equal to the end date.");
        }

        [Fact]
        public void Validate_ShouldFail_WhenPageIsZeroOrNegative()
        {
            // Arrange
            var request = new HistoricalExchangeRateRequest
            {
                BaseCurrency = "USD",
                StartDate = DateTime.UtcNow.AddDays(-10),
                EndDate = DateTime.UtcNow,
                Page = 0,
                PageSize = 10
            };

            // Act
            var result = _validator.TestValidate(request);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Page)
                  .WithErrorMessage("Page number must be greater than 0.");
        }

        [Fact]
        public void Validate_ShouldFail_WhenPageSizeIsZeroOrNegative()
        {
            // Arrange
            var request = new HistoricalExchangeRateRequest
            {
                BaseCurrency = "USD",
                StartDate = DateTime.UtcNow.AddDays(-10),
                EndDate = DateTime.UtcNow,
                Page = 1,
                PageSize = 0
            };

            // Act
            var result = _validator.TestValidate(request);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.PageSize)
                  .WithErrorMessage("Page size must be greater than 0.");
        }

        [Fact]
        public void Validate_ShouldPass_WhenValidRequest()
        {
            // Capture and truncate milliseconds to prevent minor differences
            var now = DateTime.UtcNow;
            now = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second, DateTimeKind.Utc);

            var request = new HistoricalExchangeRateRequest
            {
                BaseCurrency = "USD",
                StartDate = now.AddDays(-10),
                EndDate = now, // Ensures same exact second
                Page = 1,
                PageSize = 10
            };

            var result = _validator.TestValidate(request);

            result.ShouldNotHaveAnyValidationErrors();
        }
    }
}