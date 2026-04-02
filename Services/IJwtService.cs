using System.Security.Claims;

namespace ChatPortal.Services;

/// <summary>
/// Defines operations for generating and validating JSON Web Tokens (JWT)
/// used for authentication and iframe embed access control.
/// </summary>
public interface IJwtService
{
    /// <summary>
    /// Generates a signed JWT access token for the specified user.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="email">The email address of the user, embedded as a claim.</param>
    /// <param name="role">The role of the user (e.g. <c>"Admin"</c> or <c>"User"</c>), embedded as a claim.</param>
    /// <returns>A signed JWT string that can be used as a Bearer token.</returns>
    string GenerateAccessToken(int userId, string email, string role);

    /// <summary>
    /// Generates a cryptographically random opaque refresh token.
    /// </summary>
    /// <returns>A Base64-encoded 64-byte random string suitable for use as a refresh token.</returns>
    string GenerateRefreshToken();

    /// <summary>
    /// Validates a JWT token string and returns the associated claims principal if valid.
    /// </summary>
    /// <param name="token">The JWT token string to validate.</param>
    /// <returns>
    /// A <see cref="ClaimsPrincipal"/> containing the token's claims when validation succeeds;
    /// <see langword="null"/> if the token is invalid, expired, or cannot be parsed.
    /// </returns>
    ClaimsPrincipal? ValidateToken(string token);
}
