using CurrencyConverter.Application.Services.Security;
using AspNetCoreRateLimit;
using Serilog;
using Microsoft.OpenApi.Models;
using CurrencyConverter.Application;
using CurrencyConverter.API.DependencyInjection;
using CurrencyConverter.Infrastructure.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Host.AddLoggingServices(builder.Configuration);

builder.Services.AddControllers();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Currency Converter API", Version = "v1" });

    // Add JWT Authentication Support in Swagger UI
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter 'Bearer {your_token_here}'"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                },
                new string[] { }
            }
    });
});

builder.Services.AddAuthenticationServices(builder.Configuration);
builder.Services.AddSingleton<TokenService>();
builder.Services.AddRateLimitingServices(builder.Configuration);

builder.Services.AddApplicationServices();


// Add Missing IProcessingStrategy (Fix)
builder.Services.AddSingleton<IProcessingStrategy, AsyncKeyLockProcessingStrategy>();

// Register infrastructure services
builder.Services.AddTelemetryServices(builder.Configuration);
builder.Services.AddCachingServices();
builder.Services.AddHttpClientServices(builder.Configuration);
builder.Services.AddFactoryServices();

var app = builder.Build();

// Enable Swagger Middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Currency Converter API v1"));
}

// Enable Middleware
app.UseMiddleware<RequestLoggingMiddleware>(); // Custom request logging
app.UseSerilogRequestLogging(); // Standard Serilog request logging

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseIpRateLimiting();

app.MapControllers();
app.Run();

public partial class Program { }