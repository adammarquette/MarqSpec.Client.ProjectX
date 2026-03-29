using System.Text.Json.Serialization;

namespace MarqSpec.Client.ProjectX.Api.Models;

/// <summary>
/// Represents the response from searching for half-turn trade executions.
/// </summary>
public class SearchTradeResponse
{
    /// <summary>
    /// Gets or sets a value indicating whether the search was successful.
    /// </summary>
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the error code if the search failed.
    /// </summary>
    /// <remarks>0 = Success, 1 = AccountNotFound.</remarks>
    [JsonPropertyName("errorCode")]
    public int ErrorCode { get; set; }

    /// <summary>
    /// Gets or sets the error message if the search failed.
    /// </summary>
    [JsonPropertyName("errorMessage")]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the list of trade executions matching the search criteria.
    /// </summary>
    [JsonPropertyName("trades")]
    public List<HalfTrade> Trades { get; set; } = new();
}
