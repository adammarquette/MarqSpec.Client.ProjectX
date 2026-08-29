using System.Text.Json.Serialization;

namespace MarqSpec.Client.ProjectX.Api.Models;

/// <summary>
/// Represents a real-time trade notification from the <c>GatewayUserTrade</c> user hub event.
/// </summary>
public class TradeNotification
{
    /// <summary>Gets or sets the trade ID.</summary>
    [JsonPropertyName("id")]
    public long Id { get; set; }

    /// <summary>Gets or sets the account ID associated with the trade.</summary>
    [JsonPropertyName("accountId")]
    public int AccountId { get; set; }

    /// <summary>Gets or sets the contract ID on which the trade occurred.</summary>
    [JsonPropertyName("contractId")]
    public string ContractId { get; set; } = string.Empty;

    /// <summary>Gets or sets the timestamp when the trade was created.</summary>
    [JsonPropertyName("creationTimestamp")]
    public DateTime CreationTimestamp { get; set; }

    /// <summary>Gets or sets the price at which the trade was executed.</summary>
    [JsonPropertyName("price")]
    public decimal Price { get; set; }

    /// <summary>Gets or sets the profit and loss of the trade, if available.</summary>
    [JsonPropertyName("profitAndLoss")]
    public decimal? ProfitAndLoss { get; set; }

    /// <summary>Gets or sets the fees associated with the trade.</summary>
    [JsonPropertyName("fees")]
    public decimal Fees { get; set; }

    /// <summary>
    /// Gets or sets the side of the trade (Bid/Ask), or <c>null</c> when the venue omitted <c>side</c>.
    /// </summary>
    /// <remarks>
    /// Wire <c>0</c> is Bid and <c>1</c> is Ask. An omitted field is <c>null</c>, not
    /// <see cref="OrderSide.Bid"/> (gh#83). An explicit JSON <c>null</c> is rejected:
    /// swagger types <c>side</c> as <c>OrderSide</c>, not nullable-and-present.
    /// </remarks>
    [JsonPropertyName("side")]
    [JsonConverter(typeof(OrderSideJsonConverter))]
    public OrderSide? Side { get; set; }

    /// <summary>Gets or sets the size of the trade.</summary>
    [JsonPropertyName("size")]
    public int Size { get; set; }

    /// <summary>Gets or sets whether the trade has been voided.</summary>
    [JsonPropertyName("voided")]
    public bool Voided { get; set; }

    /// <summary>Gets or sets the order ID associated with this trade.</summary>
    [JsonPropertyName("orderId")]
    public long OrderId { get; set; }
}
