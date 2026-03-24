using System.Text.Json.Serialization;

namespace MarqSpec.Client.ProjectX.Api.Models;

/// <summary>
/// Represents a real-time trade update from the market data WebSocket stream.
/// </summary>
public class TradeUpdate
{
    /// <summary>
    /// Gets or sets the contract identifier.
    /// </summary>
    [JsonPropertyName("contractId")]
    public string ContractId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the trade identifier.
    /// </summary>
    [JsonPropertyName("tradeId")]
    public long TradeId { get; set; }

    /// <summary>
    /// Gets or sets the trade price.
    /// </summary>
    [JsonPropertyName("price")]
    public decimal Price { get; set; }

    /// <summary>
    /// Gets or sets the trade quantity.
    /// </summary>
    [JsonPropertyName("quantity")]
    public int Quantity { get; set; }

    /// <summary>
    /// Gets or sets the trade side (Buy or Sell).
    /// </summary>
    [JsonPropertyName("side")]
    public TradeSide Side { get; set; }

    /// <summary>
    /// Gets or sets the timestamp of the trade.
    /// </summary>
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets whether this trade was aggressive (taker).
    /// </summary>
    [JsonPropertyName("isAggressive")]
    public bool IsAggressive { get; set; }
}

/// <summary>
/// Represents the side of a trade.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TradeSide
{
    /// <summary>
    /// Buy side trade.
    /// </summary>
    Buy,

    /// <summary>
    /// Sell side trade.
    /// </summary>
    Sell
}
