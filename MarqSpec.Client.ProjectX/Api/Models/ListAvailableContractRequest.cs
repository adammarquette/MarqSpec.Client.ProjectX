using System.Text.Json.Serialization;

namespace MarqSpec.Client.ProjectX.Api.Models;

/// <summary>
/// Represents a request to list available contracts.
/// </summary>
public class ListAvailableContractRequest
{
    /// <summary>
    /// Gets or sets a value indicating whether to list live or simulation contracts.
    /// </summary>
    [JsonPropertyName("live")]
    public bool Live { get; set; }
}
