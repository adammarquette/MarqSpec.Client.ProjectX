using System.Text.Json.Serialization;

namespace MarqSpec.Client.ProjectX.Api.Models;

/// <summary>
/// Represents the response from searching for trading accounts.
/// </summary>
public class SearchAccountResponse
{
    /// <summary>
    /// Gets or sets a value indicating whether the search was successful.
    /// </summary>
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the error code if the search failed.
    /// </summary>
    [JsonPropertyName("errorCode")]
    public int ErrorCode { get; set; }

    /// <summary>
    /// Gets or sets the error message if the search failed.
    /// </summary>
    [JsonPropertyName("errorMessage")]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the list of trading accounts matching the search criteria.
    /// </summary>
    [JsonPropertyName("accounts")]
    public List<TradingAccount> Accounts { get; set; } = new();
}
