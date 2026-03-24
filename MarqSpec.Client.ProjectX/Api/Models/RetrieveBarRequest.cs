using System.Text.Json.Serialization;

namespace MarqSpec.Client.ProjectX.Api.Models;

/// <summary>
/// Represents a request to retrieve historical price bars.
/// </summary>
public class RetrieveBarRequest
{
    /// <summary>
    /// Gets or sets the contract ID to retrieve bars for.
    /// </summary>
    [JsonPropertyName("contractId")]
    public string ContractId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether to retrieve live or simulation data.
    /// </summary>
    [JsonPropertyName("live")]
    public bool Live { get; set; }

    /// <summary>
    /// Gets or sets the start time for the bar retrieval.
    /// </summary>
    [JsonPropertyName("startTime")]
    public DateTime StartTime { get; set; }

    /// <summary>
    /// Gets or sets the end time for the bar retrieval.
    /// </summary>
    [JsonPropertyName("endTime")]
    public DateTime EndTime { get; set; }

    /// <summary>
    /// Gets or sets the time unit for bar aggregation.
    /// </summary>
    [JsonPropertyName("unit")]
    public AggregateBarUnit Unit { get; set; }

    /// <summary>
    /// Gets or sets the number of units per bar (e.g., 5 for 5-minute bars).
    /// </summary>
    [JsonPropertyName("unitNumber")]
    public int UnitNumber { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of bars to return.
    /// </summary>
    [JsonPropertyName("limit")]
    public int Limit { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to include partial (incomplete) bars.
    /// </summary>
    [JsonPropertyName("includePartialBar")]
    public bool IncludePartialBar { get; set; }
}
