using System.Text.Json.Serialization;

namespace MarqSpec.Client.ProjectX.Api.Models;

/// <summary>
/// Represents a real-time order book update from the market data WebSocket stream.
/// </summary>
public class OrderBookUpdate
{
    /// <summary>
    /// Gets or sets the contract identifier.
    /// </summary>
    [JsonPropertyName("contractId")]
    public string ContractId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the list of bid levels (price and quantity).
    /// </summary>
    [JsonPropertyName("bids")]
    public List<OrderBookLevel> Bids { get; set; } = new();

    /// <summary>
    /// Gets or sets the list of ask levels (price and quantity).
    /// </summary>
    [JsonPropertyName("asks")]
    public List<OrderBookLevel> Asks { get; set; } = new();

    /// <summary>
    /// Gets or sets the timestamp of the order book update.
    /// </summary>
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the sequence number for ordering updates.
    /// </summary>
    [JsonPropertyName("sequenceNumber")]
    public long SequenceNumber { get; set; }
}
