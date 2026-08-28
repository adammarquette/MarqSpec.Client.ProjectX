using System.Text.Json.Serialization;

namespace MarqSpec.Client.ProjectX.Api.Models;

/// <summary>
/// Represents a real-time market trade event from the <c>GatewayTrade</c> market hub event.
/// </summary>
public class TradeUpdate
{
    /// <summary>
    /// Gets or sets the contract identifier the market hub bound this event to
    /// (for example <c>CON.F.US.EP.Z26</c>).
    /// </summary>
    /// <remarks>
    /// The hub delivers <c>(contractId, payload)</c>. <see cref="SymbolId"/> is
    /// the product root; this property is stamped from the hub argument at bind
    /// time and is not a field on the JSON payload (R-5.7, gh#86).
    /// </remarks>
    [JsonIgnore]
    public string ContractId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the symbol ID (e.g. "F.US.EP").
    /// </summary>
    [JsonPropertyName("symbolId")]
    public string SymbolId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the trade price.
    /// </summary>
    [JsonPropertyName("price")]
    public decimal Price { get; set; }

    /// <summary>
    /// Gets or sets the trade timestamp.
    /// </summary>
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the trade direction, or <c>null</c> when the venue did not
    /// send a recognised <c>type</c>.
    /// </summary>
    /// <remarks>
    /// The live wire uses <c>0</c> for Buy and <c>1</c> for Sell. An omitted,
    /// null, or unrecognised value stays <c>null</c> rather than becoming
    /// <see cref="TradeLogType.Buy"/> (R-5.8, gh#86).
    /// </remarks>
    [JsonPropertyName("type")]
    [JsonConverter(typeof(TradeLogTypeJsonConverter))]
    public TradeLogType? Type { get; set; }

    /// <summary>
    /// Gets or sets the trade volume.
    /// </summary>
    [JsonPropertyName("volume")]
    public decimal Volume { get; set; }
}
