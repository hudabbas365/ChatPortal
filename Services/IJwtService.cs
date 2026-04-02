using System.Security.Claims;

namespace ChatPortal.Services;

public interface IJwtService
{
    string GenerateAccessToken(int userId, string email, string role);
    string GenerateRefreshToken();
    ClaimsPrincipal? ValidateToken(string token);
}
