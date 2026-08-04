using Dsw2026Tpi.CrossCutting.Models;
using Dsw2026Tpi.CrossCutting.Resources;
using System.Security.Claims;
using System.Threading.RateLimiting;

namespace Dsw2026Tpi.Api.Configurations;

public static class RateLimiterConfigurationExtensions
{
    public static IServiceCollection AddAppRateLimiting(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var adminPermitLimit = configuration.GetValue<int>("RateLimiting:AdminLogin:PermitLimit");

        var adminWindowSeconds = configuration.GetValue<int>("RateLimiting:AdminLogin:WindowSeconds");

        var patientPermitLimit = configuration.GetValue<int>("RateLimiting:PatientLogin:PermitLimit");

        var patientWindowSeconds = configuration.GetValue<int>("RateLimiting:PatientLogin:WindowSeconds");

        var bookingPermitLimit = configuration.GetValue<int>("RateLimiting:PatientBooking:PermitLimit");

        var bookingWindowSeconds = configuration.GetValue<int>("RateLimiting:PatientBooking:WindowSeconds");

        var generalPermitLimit = configuration.GetValue<int>("RateLimiting:General:PermitLimit");

        var generalWindowSeconds = configuration.GetValue<int>("RateLimiting:General:WindowSeconds");

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.OnRejected = async (context, cancellationToken) =>
            {
                var logger = context.HttpContext.RequestServices
                    .GetRequiredService<ILoggerFactory>()
                    .CreateLogger("RateLimiting");

                logger.LogWarning(
                    "Rate limit excedido. Path: {Path}, IP: {IP}",
                    context.HttpContext.Request.Path,
                    context.HttpContext.Connection.RemoteIpAddress);

                //var error = new ErrorResponse(
                //    "RATE_LIMIT_EXCEEDED",
                //    "Too many requests");

                var error = new ErrorResponse(nameof(ErrorCodes.RATE_LIMIT_EXCEEDED), ErrorCodes.RATE_LIMIT_EXCEEDED);

                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;

                context.HttpContext.Response.ContentType = "application/json";

                await context.HttpContext.Response.WriteAsJsonAsync(error,cancellationToken);
            };

            // 5 solicitudes/minuto por IP
            options.AddPolicy("AdminLogin", httpContext =>
            {
                var partitionKey =
                    $"ip:{httpContext.Connection.RemoteIpAddress}";

                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey,
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = adminPermitLimit,
                        Window = TimeSpan.FromSeconds(adminWindowSeconds),
                        QueueLimit = 0
                    });
            });

            // 10 solicitudes/minuto por IP
            options.AddPolicy("PatientLogin", httpContext =>
            {
                var partitionKey =
                    $"ip:{httpContext.Connection.RemoteIpAddress}";

                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey,
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = patientPermitLimit,
                        Window = TimeSpan.FromSeconds(patientWindowSeconds),
                        QueueLimit = 0
                    });
            });

            // 5 solicitudes/minuto por paciente autenticado
            options.AddPolicy("PatientBooking", httpContext =>
            {
                var userId = httpContext.User.FindFirst(
                    ClaimTypes.NameIdentifier)?.Value;

                var partitionKey =
                    !string.IsNullOrWhiteSpace(userId)
                        ? $"user:{userId}"
                        : $"ip:{httpContext.Connection.RemoteIpAddress}";

                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey,
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = bookingPermitLimit,
                        Window = TimeSpan.FromSeconds(bookingWindowSeconds),
                        QueueLimit = 0
                    });
            });

            // 100 solicitudes/minuto por usuario autenticado o IP
            options.AddPolicy("GeneralPolicy", httpContext =>
            {
                var userId = httpContext.User.FindFirst(
                    ClaimTypes.NameIdentifier)?.Value;

                var partitionKey =
                    !string.IsNullOrWhiteSpace(userId)
                        ? $"user:{userId}"
                        : $"ip:{httpContext.Connection.RemoteIpAddress}";

                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey,
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = generalPermitLimit,
                        Window = TimeSpan.FromSeconds(generalWindowSeconds),
                        QueueLimit = 0
                    });
            });
        });

        return services;
    }
}
