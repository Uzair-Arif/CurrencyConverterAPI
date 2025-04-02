using CurrencyConverter.Application.Models.Request;
using FluentValidation;

namespace CurrencyConverter.Application.Validators;

public class ConvertCurrencyRequestValidator : AbstractValidator<ConvertCurrencyRequest>
{
    public ConvertCurrencyRequestValidator()
    {
        RuleFor(x => x.From).SetValidator(new BaseCurrencyValidator());
        RuleFor(x => x.To).SetValidator(new BaseCurrencyValidator());

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Amount must be greater than zero.");
    }
}
