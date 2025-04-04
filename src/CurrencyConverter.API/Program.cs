using CurrencyConverter.Application.Services.Security;
using AspNetCoreRateLimit;
using Serilog;
using Microsoft.OpenApi.Models;
using CurrencyConverter.Application;
using CurrencyConverter.API.DependencyInjection;
using CurrencyConverter.Infrastructure.DependencyInjection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Versioning;

var builder = WebApplication.CreateBuilder(args);

// Load environment-specific configuration
var environment = builder.Environment.EnvironmentName;

builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

// Add API Versioning
builder.Services.AddApiVersioning(options =>
{
    options.ReportApiVersions = true;
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.DefaultApiVersion = new ApiVersion(1, 0); // Default API version: v1.0
    options.ApiVersionReader = new UrlSegmentApiVersionReader(); // Enables versioning via URL
});

// Add API Explorer to support versioned endpoints in Swagger
builder.Services.AddVersionedApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV"; // Formats version as "v1", "v2"
    options.SubstituteApiVersionInUrl = true; // Replaces placeholders with version numbers
});

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
builder.Services.AddRateLimitingServices(builder.Configuration);
builder.Services.AddApplicationServices();

// Register infrastructure services
builder.Services.AddTelemetryServices(builder.Configuration);
builder.Services.AddCachingServices(builder.Configuration);
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