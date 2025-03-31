using System.Diagnostics;
using System.Security.Claims;
using Serilog.Context;

public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task Invoke(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred while processing the request.");
            throw; // Re-throw to allow the exception to propagate if necessary
        }
        finally
        {
            stopwatch.Stop();

            var clientIp = context.Connection.RemoteIpAddress?.ToString();
            var clientId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "Anonymous";
            var httpMethod = context.Request.Method;
            var endpoint = context.Request.Path;
            var responseCode = context.Response.StatusCode;
            var responseTime = stopwatch.ElapsedMilliseconds;

            using (LogContext.PushProperty("ClientIP", clientIp))
            using (LogContext.PushProperty("ClientId", clientId))
            using (LogContext.PushProperty("HttpMethod", httpMethod))
            using (LogContext.PushProperty("Endpoint", endpoint))
            using (LogContext.PushProperty("ResponseCode", responseCode))
            using (LogContext.PushProperty("ResponseTimeMs", responseTime))
            {
                _logger.LogInformation("Request: {HttpMethod} {Endpoint} | Client: {ClientId} | IP: {ClientIP} | Response: {ResponseCode} | Time: {ResponseTimeMs}ms");
            }
        }
    }
}