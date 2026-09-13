using System.Net;
using System.Text.Json;

namespace Transactions.Api.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var (statusCode, message) = exception switch
        {
            KeyNotFoundException => (
                HttpStatusCode.NotFound,
                exception.Message
            ),
            InvalidOperationException => (
                HttpStatusCode.BadRequest,
                exception.Message
            ),
            ArgumentException => (
                HttpStatusCode.BadRequest,
                exception.Message
            ),
            HttpRequestException => (
                HttpStatusCode.BadGateway,
                "A downstream service is unavailable. Please try again later."
            ),
            _ => (
                HttpStatusCode.InternalServerError,
                "An unexpected error occurred."
            )
        };

        if (statusCode is HttpStatusCode.InternalServerError or HttpStatusCode.BadGateway)
        {
            _logger.LogError(exception, "Unhandled exception occurred.");
        }

        context.Response.StatusCode = (int)statusCode;

        var response = new { error = message };
        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
}