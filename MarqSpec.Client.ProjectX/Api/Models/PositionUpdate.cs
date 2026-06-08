using System.Text.Json.Serialization;

namespace MarqSpec.Client.ProjectX.Api.Models;

/// <summary>
/// Represents a real-time position update from the <c>GatewayUserPosition</c> user hub event.
/// </summary>
public class PositionUpdate
{
    /// <summary>Gets or sets the position ID.</summary>
    [JsonPropertyName("id")]
    public int Id { get; set; }

    /// <summary>Gets or sets the account ID associated with the position.</summary>
    [JsonPropertyName("accountId")]
    public int AccountId { get; set; }

    /// <summary>Gets or sets the contract ID associated with the position.</summary>
    [JsonPropertyName("contractId")]
    public string ContractId { get; set; } = string.Empty;

    /// <summary>Gets or sets the timestamp when the position was created or opened.</summary>
    [JsonPropertyName("creationTimestamp")]
    public DateTime CreationTimestamp { get; set; }

    /// <summary>Gets or sets the position direction (Long or Short).</summary>
    [JsonPropertyName("type")]
    public PositionType Type { get; set; }

    /// <summary>Gets or sets the size of the position.</summary>
    [JsonPropertyName("size")]
    public int Size { get; set; }

    /// <summary>Gets or sets the average entry price of the position.</summary>
    [JsonPropertyName("averagePrice")]
    public decimal AveragePrice { get; set; }
}
