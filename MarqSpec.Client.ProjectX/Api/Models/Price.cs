namespace MarqSpec.Client.ProjectX.Api.Models;

/// <summary>
/// Represents the current price information for a symbol.
/// </summary>
public class Price
{
    /// <summary>
    /// Gets or sets the symbol.
    /// </summary>
    public string Symbol { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the bid price.
    /// </summary>
    public decimal Bid { get; set; }

    /// <summary>
    /// Gets or sets the ask price.
    /// </summary>
    public decimal Ask { get; set; }

    /// <summary>
    /// Gets or sets the last traded price.
    /// </summary>
    public decimal Last { get; set; }

    /// <summary>
    /// Gets or sets the trading volume.
    /// </summary>
    public decimal Volume { get; set; }

    /// <summary>
    /// Gets or sets the timestamp of the price data.
    /// </summary>
    public DateTime Timestamp { get; set; }
}
