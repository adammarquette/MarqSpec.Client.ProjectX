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
    /// Gets or sets the start time for the search range.
    /// </summary>
    [JsonPropertyName("startTime")]
    public DateTime? StartTime { get; set; }

    /// <summary>
    /// Gets or sets the end time for the search range.
    /// </summary>
    [JsonPropertyName("endTime")]
    public DateTime? EndTime { get; set; }

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
