using System.Text.Json.Serialization;

namespace MarqSpec.Client.ProjectX.Api.Models;

/// <summary>
/// Represents a trading account.
/// </summary>
public class TradingAccount
{
    /// <summary>
    /// Gets or sets the unique account identifier.
    /// </summary>
    [JsonPropertyName("id")]
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the account name.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the current account balance.
    /// </summary>
    [JsonPropertyName("balance")]
    public decimal Balance { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this account is allowed to trade.
    /// </summary>
    [JsonPropertyName("canTrade")]
    public bool CanTrade { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this account is visible.
    /// </summary>
    [JsonPropertyName("isVisible")]
    public bool IsVisible { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this is a simulated account.
    /// </summary>
    [JsonPropertyName("simulated")]
    public bool Simulated { get; set; }
}
