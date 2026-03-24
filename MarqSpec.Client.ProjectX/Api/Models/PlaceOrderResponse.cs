using System.Text.Json.Serialization;

namespace MarqSpec.Client.ProjectX.Api.Models;

/// <summary>
/// Represents the response from placing an order.
/// </summary>
public class PlaceOrderResponse
{
    /// <summary>
    /// Gets or sets a value indicating whether the order placement was successful.
    /// </summary>
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the error code if the order placement failed.
    /// </summary>
    [JsonPropertyName("errorCode")]
    public int ErrorCode { get; set; }

    /// <summary>
    /// Gets or sets the error message if the order placement failed.
    /// </summary>
    [JsonPropertyName("errorMessage")]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the order ID of the placed order.
    /// </summary>
    [JsonPropertyName("orderId")]
    public long? OrderId { get; set; }
}
