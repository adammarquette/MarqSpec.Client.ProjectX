using System.Text.Json.Serialization;

namespace MarqSpec.Client.ProjectX.Api.Models;

/// <summary>
/// Represents a request to search for trading accounts.
/// </summary>
public class SearchAccountRequest
{
    /// <summary>
    /// Gets or sets a value indicating whether to return only active accounts.
    /// </summary>
    [JsonPropertyName("onlyActiveAccounts")]
    public bool OnlyActiveAccounts { get; set; }
}
