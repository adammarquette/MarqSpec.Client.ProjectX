using System.Text.Json.Serialization;

namespace MarqSpec.Client.ProjectX.Api.Models;

/// <summary>
/// Represents a trading contract (instrument).
/// </summary>
public class Contract
{
    /// <summary>
    /// Gets or sets the unique contract identifier.
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the contract name.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the contract description.
    /// </summary>
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the minimum price increment (tick size).
    /// </summary>
    [JsonPropertyName("tickSize")]
    public decimal TickSize { get; set; }

    /// <summary>
    /// Gets or sets the monetary value of one tick.
    /// </summary>
    [JsonPropertyName("tickValue")]
    public decimal TickValue { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this is an active contract.
    /// </summary>
    [JsonPropertyName("activeContract")]
    public bool ActiveContract { get; set; }

    /// <summary>
    /// Gets or sets the symbol identifier.
    /// </summary>
    [JsonPropertyName("symbolId")]
    public string SymbolId { get; set; } = string.Empty;
}
