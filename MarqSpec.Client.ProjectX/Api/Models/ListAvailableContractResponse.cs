using System.Text.Json.Serialization;

namespace MarqSpec.Client.ProjectX.Api.Models;

/// <summary>
/// Represents the response from listing available contracts.
/// </summary>
public class ListAvailableContractResponse
{
    /// <summary>
    /// Gets or sets a value indicating whether the request was successful.
    /// </summary>
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the error code if the request failed.
    /// </summary>
    [JsonPropertyName("errorCode")]
    public int ErrorCode { get; set; }

    /// <summary>
    /// Gets or sets the error message if the request failed.
    /// </summary>
    [JsonPropertyName("errorMessage")]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the list of available contracts.
    /// </summary>
    [JsonPropertyName("contracts")]
    public List<Contract> Contracts { get; set; } = new();
}
