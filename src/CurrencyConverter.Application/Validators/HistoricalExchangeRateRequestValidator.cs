using CurrencyConverter.Application.Models.Request;
using FluentValidation;

namespace CurrencyConverter.Application.Validators;

public class HistoricalExchangeRateRequestValidator : AbstractValidator<HistoricalExchangeRateRequest>
{
    public HistoricalExchangeRateRequestValidator()
    {
        RuleFor(x => x.BaseCurrency).SetValidator(new BaseCurrencyValidator());

        RuleFor(x => x.StartDate)
            .NotEmpty().WithMessage("Start date is required.")
            .Must(date => date != default).WithMessage("Start date must be a valid date.")
            .LessThanOrEqualTo(x => x.EndDate).WithMessage("Start date must be before or equal to the end date.")
            .LessThanOrEqualTo(DateTime.UtcNow).WithMessage("Start date cannot be in the future.");

        RuleFor(x => x.EndDate)
            .NotEmpty().WithMessage("End date is required.")
            .Must(date => date != default).WithMessage("End date must be a valid date.")
            .LessThanOrEqualTo(DateTime.UtcNow).WithMessage("End date cannot be in the future.");

        RuleFor(x => x.Page)
            .GreaterThan(0).WithMessage("Page number must be greater than 0.");

        RuleFor(x => x.PageSize)
            .GreaterThan(0).WithMessage("Page size must be greater than 0.");
    }
}
