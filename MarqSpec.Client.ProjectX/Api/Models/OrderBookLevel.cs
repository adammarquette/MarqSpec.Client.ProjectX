using System.Text.Json.Serialization;

namespace MarqSpec.Client.ProjectX.Api.Models;

/// <summary>
/// Represents a single level in the order book.
/// </summary>
public class OrderBookLevel
{
    /// <summary>
    /// Gets or sets the price level.
    /// </summary>
    [JsonPropertyName("price")]
    public decimal Price { get; set; }

    /// <summary>
    /// Gets or sets the quantity at this price level.
    /// </summary>
    [JsonPropertyName("quantity")]
    public decimal Quantity { get; set; }

    /// <summary>
    /// Gets or sets the number of orders at this level.
    /// </summary>
    [JsonPropertyName("orderCount")]
    public int OrderCount { get; set; }
}
