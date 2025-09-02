using System.Text.Json;

namespace Educate.API.Middlewares;

public class RequestValidationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestValidationMiddleware> _logger;

    public RequestValidationMiddleware(
        RequestDelegate next,
        ILogger<RequestValidationMiddleware> logger
    )
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!IsValidRequest(context))
        {
            context.Response.StatusCode = 400;
            context.Response.ContentType = "application/json";

            var response = new { message = "Invalid request format" };
            await context.Response.WriteAsync(JsonSerializer.Serialize(response));
            return;
        }

        await _next(context);
    }

    private static bool IsValidRequest(HttpContext context)
    {
        if (context.Request.ContentLength > 10 * 1024 * 1024) // 10MB limit
            return false;

        return true;
    }
}
