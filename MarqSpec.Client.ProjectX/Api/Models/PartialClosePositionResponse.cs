using System.Text.Json.Serialization;

namespace MarqSpec.Client.ProjectX.Api.Models;

/// <summary>
/// Represents the response from a partial close position request.
/// </summary>
public class PartialClosePositionResponse
{
    /// <summary>
    /// Gets or sets a value indicating whether the partial close was successful.
    /// </summary>
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the error code if the partial close failed.
    /// </summary>
    /// <remarks>
    /// 0 = Success, 1 = AccountNotFound, 2 = PositionNotFound, 3 = ContractNotFound,
    /// 4 = ContractNotActive, 5 = InvalidCloseSize, 6 = OrderRejected, 7 = OrderPending,
    /// 8 = UnknownError, 9 = AccountRejected.
    /// </remarks>
    [JsonPropertyName("errorCode")]
    public int ErrorCode { get; set; }

    /// <summary>
    /// Gets or sets the error message if the partial close failed.
    /// </summary>
    [JsonPropertyName("errorMessage")]
    public string? ErrorMessage { get; set; }
}
