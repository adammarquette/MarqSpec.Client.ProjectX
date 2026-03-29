using System.Text.Json.Serialization;

namespace MarqSpec.Client.ProjectX.Api.Models;

/// <summary>
/// Represents a request to search for open (working) orders on an account.
/// </summary>
public class SearchOpenOrderRequest
{
    /// <summary>
    /// Gets or sets the account ID to search open orders for.
    /// </summary>
    [JsonPropertyName("accountId")]
    public int AccountId { get; set; }
}
