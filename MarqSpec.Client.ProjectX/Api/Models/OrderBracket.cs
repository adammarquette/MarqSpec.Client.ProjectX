using System.Text.Json.Serialization;

namespace MarqSpec.Client.ProjectX.Api.Models;

/// <summary>
/// Represents bracket order configuration for stop loss or take profit.
/// </summary>
public class OrderBracket
{
    /// <summary>
    /// Gets or sets the order type for the bracket.
    /// </summary>
    [JsonPropertyName("type")]
    public OrderType Type { get; set; }

    /// <summary>
    /// Gets or sets the limit price for the bracket order.
    /// </summary>
    [JsonPropertyName("limitPrice")]
    public decimal? LimitPrice { get; set; }

    /// <summary>
    /// Gets or sets the stop price for the bracket order.
    /// </summary>
    [JsonPropertyName("stopPrice")]
    public decimal? StopPrice { get; set; }

    /// <summary>
    /// Gets or sets the trail price for trailing stop bracket orders.
    /// </summary>
    [JsonPropertyName("trailPrice")]
    public decimal? TrailPrice { get; set; }
}
