using MarqSpec.Client.ProjectX.FakeGateway.State;
using Microsoft.AspNetCore.SignalR;

namespace MarqSpec.Client.ProjectX.FakeGateway.Hubs;

/// <summary>
/// Records the access token a hub handshake carried.
/// </summary>
/// <remarks>
/// SignalR supplies the token from <c>AccessTokenProvider</c> in one of two places depending on transport: the
/// <c>Authorization</c> header for the negotiate request and for long polling, and an <c>access_token</c> query
/// parameter for the WebSocket upgrade, because a browser WebSocket cannot set headers. Checking only one of
/// them makes the recorder report "no token" for a client that supplied one perfectly well — which is a false
/// negative in a test whose whole job is to prove the token was supplied.
/// </remarks>
public static class HubTokenRecorder
{
    /// <summary>Records whatever token <paramref name="context"/>'s handshake carried, under <paramref name="hub"/>.</summary>
    public static void Record(HubCallerContext context, GatewayState state, string hub)
    {
        var http = context.GetHttpContext();
        if (http is null)
        {
            return;
        }

        var token = http.Request.Query["access_token"].ToString();

        if (string.IsNullOrEmpty(token))
        {
            var header = http.Request.Headers.Authorization.ToString();
            if (header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                token = header[7..];
            }
        }

        if (!string.IsNullOrEmpty(token))
        {
            state.HubTokensSeen[hub] = token;
        }
    }
}
