namespace MarqSpec.Client.ProjectX.Api.Models;

/// <summary>
/// Represents a trade execution.
/// </summary>
public class Trade
{
    /// <summary>
    /// Gets or sets the unique trade identifier.
    /// </summary>
    public string TradeId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the symbol.
    /// </summary>
    public string Symbol { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the trade price.
    /// </summary>
    public decimal Price { get; set; }

    /// <summary>
    /// Gets or sets the trade quantity.
    /// </summary>
    public decimal Quantity { get; set; }

    /// <summary>
    /// Gets or sets the side of the trade (Buy or Sell).
    /// </summary>
    public string Side { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the timestamp of the trade.
    /// </summary>
    public DateTime Timestamp { get; set; }
}
