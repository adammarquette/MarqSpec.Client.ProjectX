using System.Globalization;
using MarqSpec.Client.ProjectX.FakeGateway.Auth;
using MarqSpec.Client.ProjectX.FakeGateway.State;

namespace MarqSpec.Client.ProjectX.FakeGateway.Endpoints;

/// <summary>
/// Applies armed fault scenarios and enforces the bearer token on <c>/api</c> routes.
/// </summary>
public sealed class GatewayMiddleware
{
    private readonly RequestDelegate _next;

    public GatewayMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context, GatewayState state, JwtIssuer jwt)
    {
        var path = context.Request.Path.Value ?? string.Empty;

        // The control surface and the hubs are out of scope: a test arming a fault must not have that fault
        // eaten by the very call that arms it.
        if (!path.StartsWith("/api", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        var fault = state.NextFault(path);
        if (fault is not null)
        {
            if (fault.DelayMilliseconds > 0)
            {
                await Task.Delay(fault.DelayMilliseconds, context.RequestAborted);
            }

            if (fault.Status == 429)
            {
                if (fault.RetryAfterAsHttpDate)
                {
                    var when = DateTimeOffset.UtcNow.AddSeconds(fault.RetryAfterSeconds ?? 1);
                    context.Response.Headers.RetryAfter = when.ToString("R", CultureInfo.InvariantCulture);
                }
                else if (fault.RetryAfterSeconds is { } seconds)
                {
                    context.Response.Headers.RetryAfter = seconds.ToString(CultureInfo.InvariantCulture);
                }
            }

            context.Response.StatusCode = fault.Status;
            await context.Response.WriteAsJsonAsync(new { success = false, errorCode = fault.Status, errorMessage = "injected fault" });
            return;
        }

        // loginKey is how you GET a token, so it cannot require one.
        if (!path.Contains("/Auth/loginKey", StringComparison.OrdinalIgnoreCase))
        {
            var header = context.Request.Headers.Authorization.ToString();
            var token = header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) ? header[7..] : null;

            if (!jwt.IsValid(token))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(new { success = false, errorCode = 401, errorMessage = "missing or invalid bearer token" });
                return;
            }
        }

        await _next(context);
    }
}
