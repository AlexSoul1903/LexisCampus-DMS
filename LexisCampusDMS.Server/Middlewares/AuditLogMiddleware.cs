using LexisCampusDMS.Application.Interfaces;
using LexisCampusDMS.Core.Domain.Entities;
using LexisCampusDMS.Core.Domain.Enums;
using LexisCampusDMS.Core.Domain.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace LexisCampusDMS.Server.Middlewares;

public class AuditLogMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AuditLogMiddleware> _logger;

    public AuditLogMiddleware(RequestDelegate next, ILogger<AuditLogMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(
        HttpContext context, 
        ICurrentUserService currentUserService, 
        IUnitOfWork unitOfWork)
    {
        // 1. Process pipeline
        await _next(context);

        // 2. Only audit successful operations (2xx status codes) on documents API
        if (context.Response.StatusCode >= 200 && context.Response.StatusCode < 300)
        {
            var path = context.Request.Path.Value ?? string.Empty;
            var method = context.Request.Method;

            if (path.StartsWith("/api/documents", StringComparison.OrdinalIgnoreCase) && HttpMethods.IsGet(method))
            {
                try
                {
                    AuditAction action;
                    if (path.Contains("/download", StringComparison.OrdinalIgnoreCase))
                    {
                        action = AuditAction.Downloaded;
                    }
                    else
                    {
                        action = AuditAction.Viewed;
                    }

                    // Extract DocumentId if available in route values
                    Guid? documentId = null;
                    if (context.Request.RouteValues.TryGetValue("id", out var routeVal) && 
                        routeVal is not null && 
                        Guid.TryParse(routeVal.ToString(), out var parsedId))
                    {
                        documentId = parsedId;
                    }

                    var userId = string.IsNullOrWhiteSpace(currentUserService.UserId) ? "Anonymous" : currentUserService.UserId;
                    var ipAddress = currentUserService.IpAddress ?? "127.0.0.1";
                    var details = $"AUDIT_LOG_MIDDLEWARE: {action.ToString().ToUpperInvariant()} via HTTP {method} {path}. Status: {context.Response.StatusCode}. User: {userId}. IP: {ipAddress}.";

                    var auditLog = new AuditLog(
                        userId: userId,
                        action: action,
                        documentId: documentId,
                        ipAddress: ipAddress,
                        details: details);

                    await unitOfWork.Repository<AuditLog, Guid>().AddAsync(auditLog, context.RequestAborted);
                    await unitOfWork.SaveChangesAsync(context.RequestAborted);

                    _logger.LogInformation("AuditLogMiddleware recorded {Action} event for {Path} by {UserId} from {IpAddress}",
                        action, path, userId, ipAddress);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to record audit log in AuditLogMiddleware for path {Path}", path);
                }
            }
        }
    }
}
