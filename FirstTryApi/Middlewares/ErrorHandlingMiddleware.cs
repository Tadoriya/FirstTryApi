using System.Text.Json;
using FirstTryApi.Exceptions;
using FirstTryApi.Models;

namespace FirstTryApi.Middlewares;

public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;

    public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        if (exception is GameException gameEx)
        {
            context.Response.StatusCode = gameEx.StatusCode;
            var errorResponse = new ErrorResponse(gameEx.Message, gameEx.Code);
            await context.Response.WriteAsJsonAsync(errorResponse, options);
        }
        else
        {
            context.Response.StatusCode = 500;
            _logger.LogError(exception, "An error occurred");
            var errorResponse = new ErrorResponse("Internal Server Error", "INTERNAL_SERVER_ERROR");
            await context.Response.WriteAsJsonAsync(errorResponse, options);
        }
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
}
