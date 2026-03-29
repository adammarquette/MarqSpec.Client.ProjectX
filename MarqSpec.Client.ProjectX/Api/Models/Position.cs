using System.Text.Json.Serialization;

namespace MarqSpec.Client.ProjectX.Api.Models;

/// <summary>
/// Represents an open trading position.
/// </summary>
public class Position
{
    /// <summary>
    /// Gets or sets the unique position identifier.
    /// </summary>
    [JsonPropertyName("id")]
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the account ID that owns this position.
    /// </summary>
    [JsonPropertyName("accountId")]
    public int AccountId { get; set; }

    /// <summary>
    /// Gets or sets the contract ID for this position.
    /// </summary>
    [JsonPropertyName("contractId")]
    public string ContractId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the human-readable display name of the contract.
    /// </summary>
    [JsonPropertyName("contractDisplayName")]
    public string? ContractDisplayName { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when this position was created.
    /// </summary>
    [JsonPropertyName("creationTimestamp")]
    public DateTime CreationTimestamp { get; set; }

    /// <summary>
    /// Gets or sets the position direction (Long or Short).
    /// </summary>
    [JsonPropertyName("type")]
    public PositionType Type { get; set; }

    /// <summary>
    /// Gets or sets the number of contracts in this position.
    /// </summary>
    [JsonPropertyName("size")]
    public int Size { get; set; }

    /// <summary>
    /// Gets or sets the volume-weighted average entry price.
    /// </summary>
    [JsonPropertyName("averagePrice")]
    public decimal AveragePrice { get; set; }
}
