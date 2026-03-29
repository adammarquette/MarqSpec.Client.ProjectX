using System.Text.Json.Serialization;

namespace MarqSpec.Client.ProjectX.Api.Models;

/// <summary>
/// Represents a request to search for half-turn trade executions.
/// </summary>
public class SearchTradeRequest
{
    /// <summary>
    /// Gets or sets the account ID to search trades for.
    /// </summary>
    [JsonPropertyName("accountId")]
    public int AccountId { get; set; }

    /// <summary>
    /// Gets or sets the start of the time range to search within.
    /// </summary>
    [JsonPropertyName("startTimestamp")]
    public DateTime? StartTimestamp { get; set; }

    /// <summary>
    /// Gets or sets the end of the time range to search within.
    /// </summary>
    [JsonPropertyName("endTimestamp")]
    public DateTime? EndTimestamp { get; set; }
}
