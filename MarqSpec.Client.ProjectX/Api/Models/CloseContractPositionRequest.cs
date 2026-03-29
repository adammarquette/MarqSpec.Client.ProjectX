using System.Text.Json.Serialization;

namespace MarqSpec.Client.ProjectX.Api.Models;

/// <summary>
/// Represents a request to close all contracts for a position.
/// </summary>
public class CloseContractPositionRequest
{
    /// <summary>
    /// Gets or sets the account ID that holds the position.
    /// </summary>
    [JsonPropertyName("accountId")]
    public int AccountId { get; set; }

    /// <summary>
    /// Gets or sets the contract ID of the position to close.
    /// </summary>
    [JsonPropertyName("contractId")]
    public string ContractId { get; set; } = string.Empty;
}
