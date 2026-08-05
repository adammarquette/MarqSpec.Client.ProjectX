namespace MarqSpec.Client.ProjectX.FakeGateway.State;

/// <summary>
/// A scenario a test has armed: the next <see cref="Remaining"/> matching requests fail in a specific way.
/// </summary>
/// <remarks>
/// Fault injection lives in the gateway rather than in a second stub container, because the faults that matter
/// here are about the resilience pipeline's <i>behaviour</i> — how many times it retries, whether it honours
/// <c>Retry-After</c>, and above all whether it retries an order placement at all (it must not; ADR-0002).
/// Arming a fault for N requests and then counting how many arrive is how a test proves that.
/// </remarks>
public sealed class FaultDirective
{
    /// <summary>Path suffix this applies to, e.g. <c>/api/Order/place</c>. Null matches every route.</summary>
    public string? PathSuffix { get; set; }

    /// <summary>HTTP status to return. 429 and 5xx are the interesting ones.</summary>
    public int Status { get; set; } = 500;

    /// <summary>How many more requests this directive should affect.</summary>
    public int Remaining { get; set; } = 1;

    /// <summary>When set on a 429, emitted as <c>Retry-After</c> in delta-seconds form.</summary>
    public int? RetryAfterSeconds { get; set; }

    /// <summary>When true on a 429, <c>Retry-After</c> is emitted as an HTTP-date instead.</summary>
    public bool RetryAfterAsHttpDate { get; set; }

    /// <summary>Delay before responding, for exercising timeouts.</summary>
    public int DelayMilliseconds { get; set; }

    /// <summary>Whether this directive applies to <paramref name="path"/>.</summary>
    public bool Matches(string path) =>
        PathSuffix is null || path.EndsWith(PathSuffix, StringComparison.OrdinalIgnoreCase);
}
