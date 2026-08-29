using System.Text.Json.Serialization;

namespace MarqSpec.Client.ProjectX.Api.Models;

/// <summary>
/// Represents a real-time order update from the <c>GatewayUserOrder</c> user hub event.
/// </summary>
public class OrderUpdate
{
    /// <summary>
    /// Gets or sets the order identifier.
    /// </summary>
    [JsonPropertyName("id")]
    public long Id { get; set; }

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
    /// Gets or sets the symbol identifier.
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
    /// Gets or sets the current order status.
    /// </summary>
    [JsonPropertyName("status")]
    public OrderStatus Status { get; set; }

    /// <summary>
    /// Gets or sets the order type.
    /// </summary>
    [JsonPropertyName("type")]
    public OrderType Type { get; set; }

    /// <summary>
    /// Gets or sets the order side (Bid/Ask), or <c>null</c> when the venue omitted <c>side</c>.
    /// </summary>
    /// <remarks>
    /// Wire <c>0</c> is Bid and <c>1</c> is Ask. An omitted field is <c>null</c>, not
    /// <see cref="OrderSide.Bid"/> (gh#83). An explicit JSON <c>null</c> is rejected:
    /// swagger types <c>side</c> as <c>OrderSide</c>, not nullable-and-present.
    /// </remarks>
    [JsonPropertyName("side")]
    [JsonConverter(typeof(OrderSideJsonConverter))]
    public OrderSide? Side { get; set; }

    /// <summary>
    /// Gets or sets the order size.
    /// </summary>
    [JsonPropertyName("size")]
    public int Size { get; set; }

    /// <summary>
    /// Gets or sets the limit price, if applicable.
    /// </summary>
    [JsonPropertyName("limitPrice")]
    public decimal? LimitPrice { get; set; }

    /// <summary>
    /// Gets or sets the stop price, if applicable.
    /// </summary>
    [JsonPropertyName("stopPrice")]
    public decimal? StopPrice { get; set; }

    /// <summary>
    /// Gets or sets the number of contracts filled.
    /// </summary>
    [JsonPropertyName("fillVolume")]
    public int FillVolume { get; set; }

    /// <summary>
    /// Gets or sets the price at which the order was filled, if any.
    /// </summary>
    [JsonPropertyName("filledPrice")]
    public decimal? FilledPrice { get; set; }

    /// <summary>
    /// Gets or sets the custom tag associated with the order, if any.
    /// </summary>
    [JsonPropertyName("customTag")]
    public string? CustomTag { get; set; }
}
