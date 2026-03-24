using System.Text.Json.Serialization;

namespace MarqSpec.Client.ProjectX.Api.Models;

/// <summary>
/// Represents a request to cancel an existing order.
/// </summary>
public class CancelOrderRequest
{
    /// <summary>
    /// Gets or sets the account ID for the order.
    /// </summary>
    [JsonPropertyName("accountId")]
    public int AccountId { get; set; }

    /// <summary>
    /// Gets or sets the order ID to cancel.
    /// </summary>
    [JsonPropertyName("orderId")]
    public long OrderId { get; set; }
}
