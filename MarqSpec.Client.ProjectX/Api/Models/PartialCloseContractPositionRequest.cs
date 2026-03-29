using System.Text.Json.Serialization;

namespace MarqSpec.Client.ProjectX.Api.Models;

/// <summary>
/// Represents a request to partially close a contract position.
/// </summary>
public class PartialCloseContractPositionRequest
{
    /// <summary>
    /// Gets or sets the account ID that holds the position.
    /// </summary>
    [JsonPropertyName("accountId")]
    public int AccountId { get; set; }

    /// <summary>
    /// Gets or sets the contract ID of the position to partially close.
    /// </summary>
    [JsonPropertyName("contractId")]
    public string ContractId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the number of contracts to close.
    /// </summary>
    [JsonPropertyName("size")]
    public int Size { get; set; }
}
