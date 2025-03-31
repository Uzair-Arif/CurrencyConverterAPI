using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CurrencyConverter.Application.Interfaces;
using CurrencyConverter.Application.Services.Security;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

public class TokenServiceTests
{
    private readonly ITokenService _tokenService;
    private readonly IConfiguration _configuration;

    public TokenServiceTests()
    {
        var jwtSettings = new Dictionary<string, string>
        {
            { "JwtSettings:Secret", "supersecretkey_supersecretkey_32byteslong" },  // Ensure at least 32 bytes
            { "JwtSettings:TokenExpiryMinutes", "60" },
            { "JwtSettings:Issuer", "TestIssuer" },
            { "JwtSettings:Audience", "TestAudience" }
        };

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(jwtSettings)
            .Build();

        _tokenService = new TokenService(_configuration);
    }

    [Fact]
    public void GenerateToken_ValidInputs_ReturnsValidJwt()
    {
        // Arrange
        var username = "testuser";
        var role = "Admin";

        // Act
        var token = _tokenService.GenerateToken(username, role);
        token.Should().NotBeNullOrEmpty();

        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_configuration["JwtSettings:Secret"]);

        var validationParams = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = _configuration["JwtSettings:Issuer"],
            ValidAudience = _configuration["JwtSettings:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ClockSkew = TimeSpan.Zero
        };

        // Act
        var principal = tokenHandler.ValidateToken(token, validationParams, out var validatedToken);

        // Assert
        validatedToken.Should().NotBeNull();
        validatedToken.Should().BeOfType<JwtSecurityToken>();

        var jwtToken = (JwtSecurityToken)validatedToken;

        jwtToken.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Sub && c.Value == username);
        jwtToken.Claims.Should().Contain(c =>
            (c.Type == ClaimTypes.Role || c.Type == "role") && c.Value == role); // FIXED
        jwtToken.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Jti);

        jwtToken.Issuer.Should().Be(_configuration["JwtSettings:Issuer"]);
        jwtToken.Audiences.Should().Contain(_configuration["JwtSettings:Audience"]);

        var expiry = jwtToken.ValidTo;
        expiry.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public void GenerateToken_InvalidSecret_ShouldFailValidation()
    {
        // Arrange
        var username = "testuser";
        var role = "User";
        var token = _tokenService.GenerateToken(username, role);

        var tokenHandler = new JwtSecurityTokenHandler();
        var wrongKey = Encoding.UTF8.GetBytes("wrong_secret_key_wrong_secret_key");

        var validationParams = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(wrongKey),
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = false
        };

        // Act & Assert
        Action act = () => tokenHandler.ValidateToken(token, validationParams, out _);
        act.Should().Throw<SecurityTokenException>();
    }
}