using System.Text.Json.Serialization;

namespace MarqSpec.Client.ProjectX.Api.Models;

/// <summary>
/// Represents a request to find a contract by its unique ID.
/// </summary>
public class SearchContractByIdRequest
{
    /// <summary>
    /// Gets or sets the contract ID to search for.
    /// </summary>
    [JsonPropertyName("contractId")]
    public string ContractId { get; set; } = string.Empty;
}
