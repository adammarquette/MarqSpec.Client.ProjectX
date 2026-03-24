using System.Text.Json.Serialization;

namespace MarqSpec.Client.ProjectX.Api.Models;

/// <summary>
/// Represents a real-time price update from the market data WebSocket stream.
/// </summary>
public class PriceUpdate
{
    /// <summary>
    /// Gets or sets the contract identifier.
    /// </summary>
    [JsonPropertyName("contractId")]
    public string ContractId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the last traded price.
    /// </summary>
    [JsonPropertyName("lastPrice")]
    public decimal LastPrice { get; set; }

    /// <summary>
    /// Gets or sets the bid price.
    /// </summary>
    [JsonPropertyName("bidPrice")]
    public decimal BidPrice { get; set; }

    /// <summary>
    /// Gets or sets the ask price.
    /// </summary>
    [JsonPropertyName("askPrice")]
    public decimal AskPrice { get; set; }

    /// <summary>
    /// Gets or sets the bid size (quantity).
    /// </summary>
    [JsonPropertyName("bidSize")]
    public int BidSize { get; set; }

    /// <summary>
    /// Gets or sets the ask size (quantity).
    /// </summary>
    [JsonPropertyName("askSize")]
    public int AskSize { get; set; }

    /// <summary>
    /// Gets or sets the volume traded.
    /// </summary>
    [JsonPropertyName("volume")]
    public long Volume { get; set; }

    /// <summary>
    /// Gets or sets the timestamp of the price update.
    /// </summary>
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the opening price.
    /// </summary>
    [JsonPropertyName("openPrice")]
    public decimal? OpenPrice { get; set; }

    /// <summary>
    /// Gets or sets the high price.
    /// </summary>
    [JsonPropertyName("highPrice")]
    public decimal? HighPrice { get; set; }

    /// <summary>
    /// Gets or sets the low price.
    /// </summary>
    [JsonPropertyName("lowPrice")]
    public decimal? LowPrice { get; set; }

    /// <summary>
    /// Gets or sets the previous close price.
    /// </summary>
    [JsonPropertyName("previousClose")]
    public decimal? PreviousClose { get; set; }
}
