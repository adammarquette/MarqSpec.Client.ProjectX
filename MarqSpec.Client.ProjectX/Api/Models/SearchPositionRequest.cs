using System.Text.Json.Serialization;

namespace MarqSpec.Client.ProjectX.Api.Models;

/// <summary>
/// Represents a request to search for open positions on an account.
/// </summary>
public class SearchPositionRequest
{
    /// <summary>
    /// Gets or sets the account ID to search positions for.
    /// </summary>
    [JsonPropertyName("accountId")]
    public int AccountId { get; set; }
}
