using System.Text.Json.Serialization;

namespace MarqSpec.Client.ProjectX.Api.Models;

/// <summary>
/// Represents a single half-turn (one leg) trade execution from the REST API.
/// </summary>
public class HalfTrade
{
    /// <summary>
    /// Gets or sets the unique trade identifier.
    /// </summary>
    [JsonPropertyName("id")]
    public long Id { get; set; }

    /// <summary>
    /// Gets or sets the account ID that executed this trade.
    /// </summary>
    [JsonPropertyName("accountId")]
    public int AccountId { get; set; }

    /// <summary>
    /// Gets or sets the contract ID that was traded.
    /// </summary>
    [JsonPropertyName("contractId")]
    public string ContractId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the timestamp when the trade was executed.
    /// </summary>
    [JsonPropertyName("creationTimestamp")]
    public DateTime CreationTimestamp { get; set; }

    /// <summary>
    /// Gets or sets the execution price.
    /// </summary>
    [JsonPropertyName("price")]
    public decimal Price { get; set; }

    /// <summary>
    /// Gets or sets the realized profit and loss for this trade leg.
    /// </summary>
    [JsonPropertyName("profitAndLoss")]
    public decimal? ProfitAndLoss { get; set; }

    /// <summary>
    /// Gets or sets the fees charged for this trade.
    /// </summary>
    [JsonPropertyName("fees")]
    public decimal Fees { get; set; }

    /// <summary>
    /// Gets or sets the trade side (Bid = buy, Ask = sell), or <c>null</c> when the venue omitted <c>side</c>.
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
    /// Gets or sets the number of contracts traded.
    /// </summary>
    [JsonPropertyName("size")]
    public int Size { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this trade has been voided.
    /// </summary>
    [JsonPropertyName("voided")]
    public bool Voided { get; set; }

    /// <summary>
    /// Gets or sets the ID of the order that generated this trade.
    /// </summary>
    [JsonPropertyName("orderId")]
    public long OrderId { get; set; }
}
