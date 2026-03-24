using System.Text.Json.Serialization;

namespace MarqSpec.Client.ProjectX.Api.Models;

/// <summary>
/// Represents a request to modify an existing order.
/// </summary>
public class ModifyOrderRequest
{
    /// <summary>
    /// Gets or sets the account ID for the order.
    /// </summary>
    [JsonPropertyName("accountId")]
    public int AccountId { get; set; }

    /// <summary>
    /// Gets or sets the order ID to modify.
    /// </summary>
    [JsonPropertyName("orderId")]
    public long OrderId { get; set; }

    /// <summary>
    /// Gets or sets the new limit price for the order.
    /// </summary>
    [JsonPropertyName("limitPrice")]
    public decimal? LimitPrice { get; set; }

    /// <summary>
    /// Gets or sets the new stop price for the order.
    /// </summary>
    [JsonPropertyName("stopPrice")]
    public decimal? StopPrice { get; set; }

    /// <summary>
    /// Gets or sets the new size (quantity) for the order.
    /// </summary>
    [JsonPropertyName("size")]
    public int? Size { get; set; }
}
