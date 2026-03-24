using System.Text.Json.Serialization;

namespace MarqSpec.Client.ProjectX.Api.Models;

/// <summary>
/// Represents the response from searching for contracts.
/// </summary>
public class SearchContractResponse
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
    /// Gets or sets the list of contracts matching the search criteria.
    /// </summary>
    [JsonPropertyName("contracts")]
    public List<Contract> Contracts { get; set; } = new();
}
