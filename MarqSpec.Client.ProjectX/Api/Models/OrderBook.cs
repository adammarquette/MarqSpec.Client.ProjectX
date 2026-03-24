namespace MarqSpec.Client.ProjectX.Api.Models;

/// <summary>
/// Represents the order book for a symbol.
/// </summary>
public class OrderBook
{
    /// <summary>
    /// Gets or sets the symbol.
    /// </summary>
    public string Symbol { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the bid levels.
    /// </summary>
    public IEnumerable<OrderBookLevel> Bids { get; set; } = Array.Empty<OrderBookLevel>();

    /// <summary>
    /// Gets or sets the ask levels.
    /// </summary>
    public IEnumerable<OrderBookLevel> Asks { get; set; } = Array.Empty<OrderBookLevel>();

    /// <summary>
    /// Gets or sets the timestamp of the order book data.
    /// </summary>
    public DateTime Timestamp { get; set; }
}
