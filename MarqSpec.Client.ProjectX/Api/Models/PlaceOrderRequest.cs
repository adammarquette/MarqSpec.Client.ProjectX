using System.Text.Json.Serialization;

namespace MarqSpec.Client.ProjectX.Api.Models;

/// <summary>
/// Represents a request to place a new order.
/// </summary>
public class PlaceOrderRequest
{
    /// <summary>
    /// Gets or sets the account ID for the order.
    /// </summary>
    [JsonPropertyName("accountId")]
    public int AccountId { get; set; }

    /// <summary>
    /// Gets or sets the contract ID (symbol) for the order.
    /// </summary>
    [JsonPropertyName("contractId")]
    public string ContractId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the order type.
    /// </summary>
    [JsonPropertyName("type")]
    public OrderType Type { get; set; }

    /// <summary>
    /// Gets or sets the order side (Bid/Ask).
    /// </summary>
    [JsonPropertyName("side")]
    public OrderSide Side { get; set; }

    /// <summary>
    /// Gets or sets the order size (quantity).
    /// </summary>
    [JsonPropertyName("size")]
    public int Size { get; set; }

    /// <summary>
    /// Gets or sets the limit price for limit orders.
    /// </summary>
    [JsonPropertyName("limitPrice")]
    public decimal? LimitPrice { get; set; }

    /// <summary>
    /// Gets or sets the stop price for stop orders.
    /// </summary>
    [JsonPropertyName("stopPrice")]
    public decimal? StopPrice { get; set; }

    /// <summary>
    /// Gets or sets the trail price for trailing stop orders.
    /// </summary>
    [JsonPropertyName("trailPrice")]
    public decimal? TrailPrice { get; set; }

    /// <summary>
    /// Gets or sets the custom tag for the order.
    /// </summary>
    [JsonPropertyName("customTag")]
    public string? CustomTag { get; set; }

    /// <summary>
    /// Gets or sets the stop loss bracket configuration.
    /// </summary>
    [JsonPropertyName("stopLossBracket")]
    public OrderBracket? StopLossBracket { get; set; }

    /// <summary>
    /// Gets or sets the take profit bracket configuration.
    /// </summary>
    [JsonPropertyName("takeProfitBracket")]
    public OrderBracket? TakeProfitBracket { get; set; }
}
