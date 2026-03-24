using System.Text.Json.Serialization;

namespace MarqSpec.Client.ProjectX.Api.Models;

/// <summary>
/// Represents an aggregated price bar (OHLCV - Open, High, Low, Close, Volume).
/// </summary>
public class AggregateBar
{
    /// <summary>
    /// Gets or sets the timestamp of the bar.
    /// </summary>
    [JsonPropertyName("t")]
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the opening price.
    /// </summary>
    [JsonPropertyName("o")]
    public decimal Open { get; set; }

    /// <summary>
    /// Gets or sets the highest price during the period.
    /// </summary>
    [JsonPropertyName("h")]
    public decimal High { get; set; }

    /// <summary>
    /// Gets or sets the lowest price during the period.
    /// </summary>
    [JsonPropertyName("l")]
    public decimal Low { get; set; }

    /// <summary>
    /// Gets or sets the closing price.
    /// </summary>
    [JsonPropertyName("c")]
    public decimal Close { get; set; }

    /// <summary>
    /// Gets or sets the trading volume.
    /// </summary>
    [JsonPropertyName("v")]
    public long Volume { get; set; }
}
