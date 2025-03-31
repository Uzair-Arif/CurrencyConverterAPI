using CurrencyConverter.Application.Models.Request;
using FluentValidation;

namespace CurrencyConverter.Application.Validators;

public class GetRatesRequestValidator : AbstractValidator<GetRatesRequest>
{
    public GetRatesRequestValidator()
    {
        RuleFor(x => x.BaseCurrency)
         .SetValidator(new BaseCurrencyValidator());
    }
}
