using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CurrencyConverter.Application.Interfaces;
using FluentValidation;
using CurrencyConverter.Application.Models.Request;

namespace CurrencyConverter.API.Controllers.v1
{
    [ApiController]
    [Route("api/v{version:apiVersion}/currency")]
    [ApiVersion("1.0")]
    public class CurrencyController : ControllerBase
    {
        private readonly ICurrencyService _currencyService;
        private readonly IValidator<ConvertCurrencyRequest> _convertCurrencyValidator;
        private readonly IValidator<HistoricalExchangeRateRequest> _historicalRatesValidator;
        private readonly IValidator<GetRatesRequest> _getRatesValidator;

        public CurrencyController(ICurrencyService currencyService,
                                  IValidator<ConvertCurrencyRequest> convertCurrencyValidator,
                                  IValidator<HistoricalExchangeRateRequest> historicalRatesValidator,
                                  IValidator<GetRatesRequest> getRatesValidator)
        {
            _convertCurrencyValidator = convertCurrencyValidator;
            _historicalRatesValidator = historicalRatesValidator;
            _getRatesValidator = getRatesValidator;
            _currencyService = currencyService;
        }

        [HttpGet("rates")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetRates([FromQuery] GetRatesRequest request)
        {
            var validationResult = await _getRatesValidator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                return BadRequest(validationResult.Errors);
            }

            try
            {
                var rates = await _currencyService.GetLatestRatesAsync(request.BaseCurrency, provider: request.Provider);
                return Ok(rates);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = "Currency rates not found.", error = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = "Invalid request parameters.", error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while fetching exchange rates.", error = ex.Message });
            }
        }

        [HttpPost("convert")]
        [Authorize(Roles = "User,Admin")]
        public async Task<IActionResult> ConvertCurrency([FromBody] ConvertCurrencyRequest request)
        {
            var validationResult = await _convertCurrencyValidator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                return BadRequest(validationResult.Errors);
            }

            try
            {
                var result = await _currencyService.ConvertCurrencyAsync(request.From, request.To, request.Amount, request.Provider);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = "Currency conversion rates not found.", error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while converting currency.", error = ex.Message });
            }
        }

        [HttpPost("historical")]
        [Authorize]
        public async Task<IActionResult> GetHistoricalRates([FromBody] HistoricalExchangeRateRequest request)
        {
            var validationResult = await _historicalRatesValidator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                return BadRequest(validationResult.Errors);
            }

            try
            {
                var result = await _currencyService
                    .GetHistoricalRatesAsync(request.StartDate, request.EndDate, request.BaseCurrency, request.Page, request.PageSize, request.Provider);
                if (result == null)
                {
                    return NotFound("Historical rates not found.");
                }
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = "No historical data found for the given parameters.", error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while fetching historical exchange rates.", error = ex.Message });
            }
        }
    }
}