using CurrencyConverter.Application.Models.Request;
using CurrencyConverter.Application.Validators;
using FluentValidation.TestHelper;

namespace CurrencyConverter.Tests.Application.Validators
{
    public class ConvertCurrencyRequestValidatorTests
    {
        private readonly ConvertCurrencyRequestValidator _validator;

        public ConvertCurrencyRequestValidatorTests()
        {
            _validator = new ConvertCurrencyRequestValidator();
        }

        [Theory]
        [InlineData("us", "USD", 100, "From", "Base currency must be a 3-letter code.")]
        [InlineData("USD", "usd", 100, "To", "Base currency must be in uppercase letters (e.g., USD, EUR).")]
        [InlineData("USD", "EUR", 0, "Amount", "Amount must be greater than zero.")]
        [InlineData("USD", "EUR", -10, "Amount", "Amount must be greater than zero.")]
        public void Validate_ShouldFail_ForInvalidRequest(string from, string to, decimal amount, string propertyName, string expectedErrorMessage)
        {
            // Arrange
            var request = new ConvertCurrencyRequest { From = from, To = to, Amount = amount };

            // Act
            var result = _validator.TestValidate(request);

            // Assert
            result.ShouldHaveValidationErrorFor(propertyName)
                  .WithErrorMessage(expectedErrorMessage);
        }

        [Fact]
        public void Validate_ShouldPass_WhenValidRequest()
        {
            // Arrange
            var request = new ConvertCurrencyRequest { From = "USD", To = "EUR", Amount = 100 };

            // Act
            var result = _validator.TestValidate(request);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }
    }
}