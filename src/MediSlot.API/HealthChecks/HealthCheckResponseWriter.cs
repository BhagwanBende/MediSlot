using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace MediSlot.API.HealthChecks;

public static class HealthCheckResponseWriter
{
    public static Task WriteAsync(HttpContext context, HealthReport report)
    {
        // Exception message/connection string bahercya jagat dakhvat nahit (security),
        // te fakt log madhe jatat
        var payload = new
        {
            status = report.Status.ToString(),
            totalDurationMs = (int)report.TotalDuration.TotalMilliseconds,
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                durationMs = (int)e.Value.Duration.TotalMilliseconds
            })
        };

        return context.Response.WriteAsJsonAsync(payload);
    }
}