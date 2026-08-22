using System.Text.Json.Serialization;

namespace MarqSpec.Client.ProjectX.Api.Models;

/// <summary>
/// Represents a request to search for orders within a time range.
/// </summary>
public class SearchOrderRequest
{
    /// <summary>
    /// Gets or sets the account ID to search orders for.
    /// </summary>
    [JsonPropertyName("accountId")]
    public int AccountId { get; set; }

    /// <summary>
    /// Gets or sets the start of the search range. <b>The gateway requires this</b> — its
    /// <c>/api/Order/search</c> schema lists <c>startTimestamp</c> under <c>required</c>. Leaving it null lets
    /// the gateway apply a window of its own choosing, and an order outside that window comes back simply
    /// absent, which a caller cannot tell apart from "no such order".
    /// </summary>
    /// <remarks>
    /// The CLR name is <c>StartTime</c> and the wire name is <c>startTimestamp</c>, deliberately. The wire name
    /// is what the gateway's schema dictates; the CLR name matches the <c>startTime</c> parameter on
    /// <c>IProjectXApiClient.GetOrdersAsync</c>, which is the surface a consumer actually calls. Sending
    /// <c>startTime</c> on the wire — as this client did through v1.0.5 — left the required field absent, so
    /// the window was never applied (gh#642).
    /// </remarks>
    [JsonPropertyName("startTimestamp")]
    public DateTime? StartTime { get; set; }

    /// <summary>
    /// Gets or sets the end of the search range. The gateway names this <c>endTimestamp</c> on the wire; the
    /// CLR name matches the client's <c>endTime</c> parameter. Optional — the schema does not require it.
    /// </summary>
    [JsonPropertyName("endTimestamp")]
    public DateTime? EndTime { get; set; }

    /// <summary>
    /// Gets or sets the contract ID (symbol) to filter by.
    /// </summary>
    /// <remarks>
    /// <b>Not part of the gateway's <c>SearchOrderRequest</c> schema.</b> It is serialized and the gateway
    /// ignores it, so it filters nothing. Filter the returned collection instead.
    /// </remarks>
    [JsonPropertyName("contractId")]
    public string? ContractId { get; set; }

    /// <summary>
    /// Gets or sets the order status to filter by.
    /// </summary>
    /// <remarks>
    /// <b>Not part of the gateway's <c>SearchOrderRequest</c> schema.</b> It is serialized and the gateway
    /// ignores it, so it filters nothing. Filter the returned collection instead.
    /// </remarks>
    [JsonPropertyName("status")]
    public OrderStatus? Status { get; set; }
}
