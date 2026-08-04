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
    /// Gets or sets the start of the search range. The gateway's <c>/api/Order/search</c> schema names this
    /// <c>startTimestamp</c> (and requires it) — matching <see cref="SearchTradeRequest.StartTimestamp"/>. Sending
    /// <c>startTime</c> here left the required field absent, so the window was never applied.
    /// </summary>
    [JsonPropertyName("startTimestamp")]
    public DateTime? StartTimestamp { get; set; }

    /// <summary>
    /// Gets or sets the end of the search range. The gateway names this <c>endTimestamp</c>, not <c>endTime</c>.
    /// </summary>
    [JsonPropertyName("endTimestamp")]
    public DateTime? EndTimestamp { get; set; }

    /// <summary>
    /// Gets or sets the contract ID (symbol) to filter by.
    /// </summary>
    [JsonPropertyName("contractId")]
    public string? ContractId { get; set; }

    /// <summary>
    /// Gets or sets the order status to filter by.
    /// </summary>
    [JsonPropertyName("status")]
    public OrderStatus? Status { get; set; }
}
