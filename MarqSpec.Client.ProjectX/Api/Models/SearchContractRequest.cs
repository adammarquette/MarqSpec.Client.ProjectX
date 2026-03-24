using System.Text.Json.Serialization;

namespace MarqSpec.Client.ProjectX.Api.Models;

/// <summary>
/// Represents a request to search for contracts.
/// </summary>
public class SearchContractRequest
{
    /// <summary>
    /// Gets or sets the search text to filter contracts.
    /// </summary>
    [JsonPropertyName("searchText")]
    public string? SearchText { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to search live or simulation contracts.
    /// </summary>
    [JsonPropertyName("live")]
    public bool Live { get; set; }
}
