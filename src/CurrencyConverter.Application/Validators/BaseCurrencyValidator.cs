using FluentValidation;

namespace CurrencyConverter.Application.Validators;

public class BaseCurrencyValidator : AbstractValidator<string>
{
    public BaseCurrencyValidator()
    {
        RuleFor(x => x)
            .NotEmpty().WithMessage("Base currency is required.")
            .Length(3).WithMessage("Base currency must be a 3-letter code.")
            .Matches("^[A-Z]{3}$").WithMessage("Base currency must be in uppercase letters (e.g., USD, EUR).");
    }
}
