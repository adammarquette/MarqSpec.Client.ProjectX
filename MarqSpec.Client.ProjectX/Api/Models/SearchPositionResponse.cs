using System.Text.Json.Serialization;

namespace MarqSpec.Client.ProjectX.Api.Models;

/// <summary>
/// Represents the response from searching for open positions.
/// </summary>
public class SearchPositionResponse
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
    /// Gets or sets the list of open positions matching the search criteria.
    /// </summary>
    [JsonPropertyName("positions")]
    public List<Position> Positions { get; set; } = new();
}
