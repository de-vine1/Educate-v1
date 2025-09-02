using System.Security.Claims;
using Educate.Application.Interfaces;

namespace Educate.API.Middlewares;

public class AuditMiddleware
{
    private readonly RequestDelegate _next;

    public AuditMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IAuditService auditService)
    {
        await _next(context);

        if (ShouldAudit(context))
        {
            var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "Anonymous";
            var action = $"{context.Request.Method} {context.Request.Path}";
            var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
            var userAgent = context.Request.Headers.UserAgent.ToString();

            await auditService.LogAsync(
                userId,
                action,
                $"Status: {context.Response.StatusCode}",
                ipAddress,
                userAgent
            );
        }
    }

    private static bool ShouldAudit(HttpContext context)
    {
        var path = context.Request.Path.Value?.ToLower();
        return path?.Contains("login") == true
            || path?.Contains("payment") == true
            || path?.Contains("subscribe") == true
            || context.Request.Method == "POST"
            || context.Request.Method == "PUT"
            || context.Request.Method == "DELETE";
    }
}
