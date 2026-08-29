using System.Text.Json.Serialization;

namespace MarqSpec.Client.ProjectX.Api.Models;

/// <summary>
/// Represents a single DOM (Depth of Market) update from the <c>GatewayDepth</c> market hub event.
/// </summary>
public class OrderBookUpdate
{
    /// <summary>
    /// Gets or sets the contract identifier the market hub bound this event to
    /// (for example <c>CON.F.US.EP.Z26</c>).
    /// </summary>
    /// <remarks>
    /// The hub delivers <c>(contractId, payload)</c>. The depth payload has no
    /// symbol field; this property is stamped from the hub argument at bind
    /// time and is not a field on the JSON payload (R-5.7, gh#86).
    /// </remarks>
    [JsonIgnore]
    public string ContractId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the timestamp of the DOM update.
    /// </summary>
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the DOM entry type.
    /// </summary>
    [JsonPropertyName("type")]
    public DomType Type { get; set; }

    /// <summary>
    /// Gets or sets the price level.
    /// </summary>
    [JsonPropertyName("price")]
    public decimal Price { get; set; }

    /// <summary>
    /// Gets or sets the total volume at this price level.
    /// </summary>
    [JsonPropertyName("volume")]
    public decimal Volume { get; set; }

    /// <summary>
    /// Gets or sets the current (delta) volume at this price level.
    /// </summary>
    [JsonPropertyName("currentVolume")]
    public int CurrentVolume { get; set; }
}

/// <summary>
/// Identifies the type of a DOM (Depth of Market) update entry.
/// </summary>
public enum DomType
{
    /// <summary>Unknown type.</summary>
    Unknown = 0,
    /// <summary>Ask level.</summary>
    Ask = 1,
    /// <summary>Bid level.</summary>
    Bid = 2,
    /// <summary>Best ask level.</summary>
    BestAsk = 3,
    /// <summary>Best bid level.</summary>
    BestBid = 4,
    /// <summary>Trade at this level.</summary>
    Trade = 5,
    /// <summary>Full book reset.</summary>
    Reset = 6,
    /// <summary>Session low.</summary>
    Low = 7,
    /// <summary>Session high.</summary>
    High = 8,
    /// <summary>New best bid.</summary>
    NewBestBid = 9,
    /// <summary>New best ask.</summary>
    NewBestAsk = 10,
    /// <summary>Fill at this level.</summary>
    Fill = 11,
}
