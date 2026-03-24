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
}
