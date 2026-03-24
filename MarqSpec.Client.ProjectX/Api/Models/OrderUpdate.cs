using System.Text.Json.Serialization;

namespace MarqSpec.Client.ProjectX.Api.Models;

/// <summary>
/// Represents a real-time order update from the user WebSocket stream.
/// </summary>
public class OrderUpdate
{
    /// <summary>
    /// Gets or sets the order identifier.
    /// </summary>
    [JsonPropertyName("orderId")]
    public long OrderId { get; set; }

    /// <summary>
    /// Gets or sets the account identifier.
    /// </summary>
    [JsonPropertyName("accountId")]
    public int AccountId { get; set; }

    /// <summary>
    /// Gets or sets the contract identifier.
    /// </summary>
    [JsonPropertyName("contractId")]
    public string ContractId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the order status.
    /// </summary>
    [JsonPropertyName("status")]
    public OrderStatus Status { get; set; }

    /// <summary>
    /// Gets or sets the order type.
    /// </summary>
    [JsonPropertyName("type")]
    public OrderType Type { get; set; }

    /// <summary>
    /// Gets or sets the order side.
    /// </summary>
    [JsonPropertyName("side")]
    public OrderSide Side { get; set; }

    /// <summary>
    /// Gets or sets the order size.
    /// </summary>
    [JsonPropertyName("size")]
    public int Size { get; set; }

    /// <summary>
    /// Gets or sets the filled quantity.
    /// </summary>
    [JsonPropertyName("filledQuantity")]
    public int FilledQuantity { get; set; }

    /// <summary>
    /// Gets or sets the remaining quantity.
    /// </summary>
    [JsonPropertyName("remainingQuantity")]
    public int RemainingQuantity { get; set; }

    /// <summary>
    /// Gets or sets the limit price (for limit orders).
    /// </summary>
    [JsonPropertyName("limitPrice")]
    public decimal? LimitPrice { get; set; }

    /// <summary>
    /// Gets or sets the stop price (for stop orders).
    /// </summary>
    [JsonPropertyName("stopPrice")]
    public decimal? StopPrice { get; set; }

    /// <summary>
    /// Gets or sets the average fill price.
    /// </summary>
    [JsonPropertyName("averageFillPrice")]
    public decimal? AverageFillPrice { get; set; }

    /// <summary>
    /// Gets or sets the timestamp of the order update.
    /// </summary>
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the update reason or message.
    /// </summary>
    [JsonPropertyName("message")]
    public string? Message { get; set; }

    /// <summary>
    /// Gets or sets the rejection reason if the order was rejected.
    /// </summary>
    [JsonPropertyName("rejectionReason")]
    public string? RejectionReason { get; set; }
}
