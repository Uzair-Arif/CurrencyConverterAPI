using CurrencyConverter.API.DependencyInjection;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace CurrencyConverter.Tests.DependencyInjection;

public class AuthenticationServiceExtensionsTests
{
    [Fact]
    public void AddAuthenticationServices_Should_RegisterJwtAuthentication()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
            { "JwtSettings:Secret", "supersecretkey12345" },
            { "JwtSettings:Issuer", "TestIssuer" },
            { "JwtSettings:Audience", "TestAudience" }
            })
            .Build();

        // Act
        services.AddAuthenticationServices(configuration);
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var authenticationSchemeProvider = serviceProvider.GetService<IAuthenticationSchemeProvider>();
        authenticationSchemeProvider.Should().NotBeNull("because JWT authentication should be registered");

        var tokenValidationParameters = serviceProvider.GetService<TokenValidationParameters>();
        tokenValidationParameters.Should().NotBeNull("because token validation parameters should be configured");
        tokenValidationParameters.ValidIssuer.Should().Be("TestIssuer");
        tokenValidationParameters.ValidAudience.Should().Be("TestAudience");
        tokenValidationParameters.IssuerSigningKey.Should().BeOfType<SymmetricSecurityKey>()
            .Which.Key.Should().BeEquivalentTo(Encoding.UTF8.GetBytes("supersecretkey12345"));
    }

    [Fact]
    public void AddAuthenticationServices_Should_RegisterAuthorizationPolicies()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                { "JwtSettings:Secret", "supersecretkey12345" },
                { "JwtSettings:Issuer", "TestIssuer" },
                { "JwtSettings:Audience", "TestAudience" }
            })
            .Build();

        // Act
        services.AddAuthenticationServices(configuration);
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var authorizationPolicyProvider = serviceProvider.GetService<IAuthorizationPolicyProvider>();
        authorizationPolicyProvider.Should().NotBeNull("because authorization policies should be registered");

        // Verify specific policies
        var adminPolicy = authorizationPolicyProvider.GetPolicyAsync("AdminOnly").Result;
        adminPolicy.Should().NotBeNull("because 'AdminOnly' policy should be registered");
        adminPolicy.Requirements.Should().ContainSingle()
            .Which.Should().BeOfType<RolesAuthorizationRequirement>()
            .Which.AllowedRoles.Should().Contain("Admin");

        var userPolicy = authorizationPolicyProvider.GetPolicyAsync("UserOnly").Result;
        userPolicy.Should().NotBeNull("because 'UserOnly' policy should be registered");
        userPolicy.Requirements.Should().ContainSingle()
            .Which.Should().BeOfType<RolesAuthorizationRequirement>()
            .Which.AllowedRoles.Should().Contain("User");
    }
}