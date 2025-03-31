using CurrencyConverter.Application.Validators;
using FluentValidation.TestHelper;

namespace CurrencyConverter.Tests.Application.Validators
{
    public class BaseCurrencyValidatorTests
    {
        private readonly BaseCurrencyValidator _validator;

        public BaseCurrencyValidatorTests()
        {
            _validator = new BaseCurrencyValidator();
        }

        [Theory]
        [InlineData("USD")]
        [InlineData("EUR")]
        [InlineData("GBP")]
        public void Validate_ShouldPass_ForValidBaseCurrency(string currency)
        {
            // Act
            var result = _validator.TestValidate(currency);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Theory]
        [InlineData("", "Base currency is required.")]
        [InlineData("US", "Base currency must be a 3-letter code.")]
        [InlineData("USDE", "Base currency must be a 3-letter code.")]
        [InlineData("EU", "Base currency must be a 3-letter code.")]
        [InlineData("usd", "Base currency must be in uppercase letters (e.g., USD, EUR).")]
        [InlineData("eur", "Base currency must be in uppercase letters (e.g., USD, EUR).")]
        [InlineData("gBp", "Base currency must be in uppercase letters (e.g., USD, EUR).")]
        [InlineData("123", "Base currency must be in uppercase letters (e.g., USD, EUR).")]
        [InlineData("A1C", "Base currency must be in uppercase letters (e.g., USD, EUR).")]
        [InlineData("U$D", "Base currency must be in uppercase letters (e.g., USD, EUR).")]
        public void Validate_ShouldFail_ForInvalidBaseCurrency(string currency, string expectedErrorMessage)
        {
            // Act
            var result = _validator.TestValidate(currency);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x)
                .WithErrorMessage(expectedErrorMessage);
        }
    }
}