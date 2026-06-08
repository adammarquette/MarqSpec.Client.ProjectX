using System.Text.Json.Serialization;

namespace MarqSpec.Client.ProjectX.Api.Models;

/// <summary>
/// Represents a real-time account update from the <c>GatewayUserAccount</c> user hub event.
/// </summary>
public class AccountUpdate
{
    /// <summary>Gets or sets the account ID.</summary>
    [JsonPropertyName("id")]
    public int Id { get; set; }

    /// <summary>Gets or sets the account name.</summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the current account balance.</summary>
    [JsonPropertyName("balance")]
    public decimal Balance { get; set; }

    /// <summary>Gets or sets whether the account is eligible for trading.</summary>
    [JsonPropertyName("canTrade")]
    public bool CanTrade { get; set; }

    /// <summary>Gets or sets whether the account is visible.</summary>
    [JsonPropertyName("isVisible")]
    public bool IsVisible { get; set; }

    /// <summary>Gets or sets whether the account is simulated.</summary>
    [JsonPropertyName("simulated")]
    public bool Simulated { get; set; }
}
