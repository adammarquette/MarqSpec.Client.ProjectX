using System.Text.Json.Serialization;

namespace MarqSpec.Client.ProjectX.Api.Models;

/// <summary>
/// Represents a real-time market trade event from the <c>GatewayTrade</c> market hub event.
/// </summary>
public class TradeUpdate
{
    /// <summary>
    /// Gets or sets the symbol ID (e.g. "F.US.EP").
    /// </summary>
    [JsonPropertyName("symbolId")]
    public string SymbolId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the trade price.
    /// </summary>
    [JsonPropertyName("price")]
    public decimal Price { get; set; }

    /// <summary>
    /// Gets or sets the trade timestamp.
    /// </summary>
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the trade direction.
    /// </summary>
    [JsonPropertyName("type")]
    public TradeLogType Type { get; set; }

    /// <summary>
    /// Gets or sets the trade volume.
    /// </summary>
    [JsonPropertyName("volume")]
    public decimal Volume { get; set; }
}

/// <summary>
/// Represents the direction of a market trade log entry.
/// </summary>
public enum TradeLogType
{
    /// <summary>Buy-side trade.</summary>
    Buy = 0,
    /// <summary>Sell-side trade.</summary>
    Sell = 1,
}
