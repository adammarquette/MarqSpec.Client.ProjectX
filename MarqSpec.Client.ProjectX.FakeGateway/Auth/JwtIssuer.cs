using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace MarqSpec.Client.ProjectX.FakeGateway.Auth;

/// <summary>
/// Issues and validates real HS256 JSON Web Tokens.
/// </summary>
/// <remarks>
/// Hand-rolled rather than pulled from a package, for two reasons. The fake stays dependency-light, and — more
/// usefully — the token it issues is a genuine JWT with a real <c>exp</c> claim. The client does not currently
/// parse <c>exp</c> (it assumes 55 minutes; see ADR-0003), so issuing a token with a *short, controllable*
/// lifetime is what will let that follow-up be tested when someone takes it on.
/// </remarks>
public sealed class JwtIssuer
{
    private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);
    private readonly byte[] _key = Encoding.UTF8.GetBytes("fake-gateway-signing-key-not-a-secret-and-never-used-anywhere-real");

    /// <summary>Lifetime stamped into the <c>exp</c> claim of newly issued tokens.</summary>
    public TimeSpan TokenLifetime { get; set; } = TimeSpan.FromHours(1);

    /// <summary>Issues a signed token for <paramref name="subject"/>.</summary>
    public string Issue(string subject)
    {
        var issuedAt = DateTimeOffset.UtcNow;
        var header = new Dictionary<string, object> { ["alg"] = "HS256", ["typ"] = "JWT" };
        var payload = new Dictionary<string, object>
        {
            ["sub"] = subject,
            ["iss"] = "marqspec-fake-gateway",
            ["iat"] = issuedAt.ToUnixTimeSeconds(),
            ["exp"] = issuedAt.Add(TokenLifetime).ToUnixTimeSeconds(),
        };

        var encodedHeader = Base64Url(JsonSerializer.SerializeToUtf8Bytes(header, _json));
        var encodedPayload = Base64Url(JsonSerializer.SerializeToUtf8Bytes(payload, _json));
        var signingInput = $"{encodedHeader}.{encodedPayload}";

        return $"{signingInput}.{Base64Url(Sign(signingInput))}";
    }

    /// <summary>
    /// Whether <paramref name="token"/> was issued by this instance and has not expired.
    /// </summary>
    public bool IsValid(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        var parts = token.Split('.');
        if (parts.Length != 3)
        {
            return false;
        }

        var expected = Base64Url(Sign($"{parts[0]}.{parts[1]}"));
        if (!CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(expected), Encoding.UTF8.GetBytes(parts[2])))
        {
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(FromBase64Url(parts[1]));
            return document.RootElement.TryGetProperty("exp", out var exp)
                && exp.GetInt64() > DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private byte[] Sign(string value) => HMACSHA256.HashData(_key, Encoding.UTF8.GetBytes(value));

    private static string Base64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static byte[] FromBase64Url(string value)
    {
        var padded = value.Replace('-', '+').Replace('_', '/');
        padded += new string('=', (4 - (padded.Length % 4)) % 4);
        return Convert.FromBase64String(padded);
    }
}
