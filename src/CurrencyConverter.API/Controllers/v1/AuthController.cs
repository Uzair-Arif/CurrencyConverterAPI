using Microsoft.AspNetCore.Mvc;
using CurrencyConverter.API.Models;
using CurrencyConverter.Application.Interfaces;

namespace CurrencyConverter.API.Controllers.v1
{
    [ApiController]
    [Route("api/v{version:apiVersion}/auth")]
    [ApiVersion("1.0")]
    public class AuthController : ControllerBase
    {
        private readonly ITokenService _tokenService;

        public AuthController(ITokenService tokenService)
        {
            _tokenService = tokenService;
        }

        private static readonly List<(string Username, string Password, string Role)> Users = new()
        {
            ("admin", "Admin123!", "Admin"),
            ("user", "User123!", "User")
        };

        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequest request)
        {
            var user = Users.FirstOrDefault(u => u.Username == request.Username && u.Password == request.Password);
            if (user == default)
            {
                return Unauthorized("Invalid credentials");
            }

            var token = _tokenService.GenerateToken(user.Username, user.Role);
            return Ok(new { Token = token });
        }
    }
}