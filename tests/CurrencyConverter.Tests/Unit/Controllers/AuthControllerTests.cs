using System.Net;
using CurrencyConverter.API.Controllers.v1;
using CurrencyConverter.API.Models;
using CurrencyConverter.Application.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace CurrencyConverter.Tests.Controllers
{
    public class AuthControllerTests
    {
        private readonly AuthController _controller;
        private readonly ITokenService _mockTokenService;

        public AuthControllerTests()
        {
            _mockTokenService = Substitute.For<ITokenService>();
            _controller = new AuthController(_mockTokenService);
        }

        [Fact]
        public void Login_WhenValidCredentials_ReturnsOkWithToken()
        {
            // Arrange
            var request = new LoginRequest { Username = "admin", Password = "Admin123!" };
            _mockTokenService.GenerateToken("admin", "Admin").Returns("mocked-jwt-token");

            // Act
            var result = _controller.Login(request) as OkObjectResult;

            // Assert
            result.Should().NotBeNull();
            result.StatusCode.Should().Be((int)HttpStatusCode.OK);
            result.Value.Should().BeEquivalentTo(new { Token = "mocked-jwt-token" });
        }

        [Fact]
        public void Login_WhenInvalidCredentials_ReturnsUnauthorized()
        {
            // Arrange
            var request = new LoginRequest { Username = "wronguser", Password = "wrongpass" };

            // Act
            var result = _controller.Login(request) as UnauthorizedObjectResult;

            // Assert
            result.Should().NotBeNull();
            result.StatusCode.Should().Be((int)HttpStatusCode.Unauthorized);
            result.Value.Should().Be("Invalid credentials");
        }
    }
}