namespace MarqSpec.Client.ProjectX.Authentication;

/// <summary>
/// Provides authentication services for the ProjectX API.
/// </summary>
public interface IAuthenticationService
{
    /// <summary>
    /// Gets the current access token, refreshing it if necessary.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The access token.</returns>
    Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Refreshes the access token.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    Task RefreshTokenAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates the current session token. If the server returns a new token it is stored automatically.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns><see langword="true"/> if the session is valid; <see langword="false"/> if it has expired or is invalid.</returns>
    Task<bool> ValidateSessionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Logs out the current session and clears the locally stored token.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    Task LogoutAsync(CancellationToken cancellationToken = default);
}
