using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace FirstTryApi.Middlewares;

public class LoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<LoggingMiddleware> _logger;

    public LoggingMiddleware(RequestDelegate next, ILogger<LoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        _logger.LogDebug("HTTP {Method} {Path} started",
            context.Request.Method, context.Request.Path);

        await _next(context);

        _logger.LogDebug("HTTP {Method} {Path} finished with {StatusCode}",
            context.Request.Method, context.Request.Path, context.Response.StatusCode);
    }
}
