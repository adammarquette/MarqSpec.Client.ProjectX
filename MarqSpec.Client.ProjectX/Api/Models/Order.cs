using System.Text.Json.Serialization;

namespace MarqSpec.Client.ProjectX.Api.Models;

/// <summary>
/// Represents a trading order.
/// </summary>
public class Order
{
    /// <summary>
    /// Gets or sets the unique order ID.
    /// </summary>
    [JsonPropertyName("id")]
    public long Id { get; set; }

    /// <summary>
    /// Gets or sets the account ID that owns this order.
    /// </summary>
    [JsonPropertyName("accountId")]
    public int AccountId { get; set; }

    /// <summary>
    /// Gets or sets the contract ID (symbol) for the order.
    /// </summary>
    [JsonPropertyName("contractId")]
    public string ContractId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the symbol ID for the order.
    /// </summary>
    [JsonPropertyName("symbolId")]
    public string SymbolId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the timestamp when the order was created.
    /// </summary>
    [JsonPropertyName("creationTimestamp")]
    public DateTime CreationTimestamp { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the order was last updated.
    /// </summary>
    [JsonPropertyName("updateTimestamp")]
    public DateTime? UpdateTimestamp { get; set; }

    /// <summary>
    /// Gets or sets the current status of the order.
    /// </summary>
    [JsonPropertyName("status")]
    public OrderStatus Status { get; set; }

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
    /// Gets or sets the filled volume (how many contracts have been filled).
    /// </summary>
    [JsonPropertyName("fillVolume")]
    public int FillVolume { get; set; }

    /// <summary>
    /// Gets or sets the average filled price.
    /// </summary>
    [JsonPropertyName("filledPrice")]
    public decimal? FilledPrice { get; set; }

    /// <summary>
    /// Gets or sets the custom tag for the order.
    /// </summary>
    [JsonPropertyName("customTag")]
    public string? CustomTag { get; set; }
}
