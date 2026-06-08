using System.Text.Json.Serialization;

namespace MarqSpec.Client.ProjectX.Api.Models;

/// <summary>
/// Represents a real-time quote update from the <c>GatewayQuote</c> market hub event.
/// </summary>
public class PriceUpdate
{
    /// <summary>
    /// Gets or sets the symbol ID (e.g. "F.US.EP").
    /// </summary>
    [JsonPropertyName("symbol")]
    public string Symbol { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the friendly symbol name (currently unused by the server).
    /// </summary>
    [JsonPropertyName("symbolName")]
    public string? SymbolName { get; set; }

    /// <summary>
    /// Gets or sets the last traded price.
    /// </summary>
    [JsonPropertyName("lastPrice")]
    public decimal LastPrice { get; set; }

    /// <summary>
    /// Gets or sets the current best bid price.
    /// </summary>
    [JsonPropertyName("bestBid")]
    public decimal BestBid { get; set; }

    /// <summary>
    /// Gets or sets the current best ask price.
    /// </summary>
    [JsonPropertyName("bestAsk")]
    public decimal BestAsk { get; set; }

    /// <summary>
    /// Gets or sets the price change since previous close.
    /// </summary>
    [JsonPropertyName("change")]
    public decimal Change { get; set; }

    /// <summary>
    /// Gets or sets the percent change since previous close.
    /// </summary>
    [JsonPropertyName("changePercent")]
    public decimal ChangePercent { get; set; }

    /// <summary>
    /// Gets or sets the opening price for the session.
    /// </summary>
    [JsonPropertyName("open")]
    public decimal Open { get; set; }

    /// <summary>
    /// Gets or sets the session high price.
    /// </summary>
    [JsonPropertyName("high")]
    public decimal High { get; set; }

    /// <summary>
    /// Gets or sets the session low price.
    /// </summary>
    [JsonPropertyName("low")]
    public decimal Low { get; set; }

    /// <summary>
    /// Gets or sets the total traded volume for the session.
    /// </summary>
    [JsonPropertyName("volume")]
    public decimal Volume { get; set; }

    /// <summary>
    /// Gets or sets the last updated timestamp.
    /// </summary>
    [JsonPropertyName("lastUpdated")]
    public DateTime LastUpdated { get; set; }

    /// <summary>
    /// Gets or sets the quote timestamp.
    /// </summary>
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }
}
