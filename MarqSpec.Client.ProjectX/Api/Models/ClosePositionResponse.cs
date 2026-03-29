using System.Text.Json.Serialization;

namespace MarqSpec.Client.ProjectX.Api.Models;

/// <summary>
/// Represents the response from a close position request.
/// </summary>
public class ClosePositionResponse
{
    /// <summary>
    /// Gets or sets a value indicating whether the close was successful.
    /// </summary>
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the error code if the close failed.
    /// </summary>
    /// <remarks>
    /// 0 = Success, 1 = AccountNotFound, 2 = PositionNotFound, 3 = ContractNotFound,
    /// 4 = ContractNotActive, 5 = OrderRejected, 6 = OrderPending, 7 = UnknownError, 8 = AccountRejected.
    /// </remarks>
    [JsonPropertyName("errorCode")]
    public int ErrorCode { get; set; }

    /// <summary>
    /// Gets or sets the error message if the close failed.
    /// </summary>
    [JsonPropertyName("errorMessage")]
    public string? ErrorMessage { get; set; }
}
