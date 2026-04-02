using System.Security.Cryptography;
using System.Text;

namespace ChatPortal.Services;

public class UserService : IUserService
{
    private readonly IJwtService _jwtService;

    // In-memory store for demo (no DB required)
    private static readonly List<(int Id, string FirstName, string LastName, string Email, string PasswordHash, string Role)> _users = new()
    {
        (1, "Admin", "User", "admin@chatportal.com", HashPassword("Admin@123"), "Admin"),
        (2, "Demo", "User", "demo@chatportal.com", HashPassword("Demo@123"), "User")
    };

    public UserService(IJwtService jwtService)
    {
        _jwtService = jwtService;
    }

    public Task<LoginResult> LoginAsync(string email, string password)
    {
        var hash = HashPassword(password);
        var user = _users.FirstOrDefault(u => u.Email == email && u.PasswordHash == hash);
        if (user == default)
            return Task.FromResult(new LoginResult(false, null, null, "Invalid email or password."));

        var accessToken = _jwtService.GenerateAccessToken(user.Id, user.Email, user.Role);
        var refreshToken = _jwtService.GenerateRefreshToken();
        return Task.FromResult(new LoginResult(true, accessToken, refreshToken, null));
    }

    public Task<RegisterResult> RegisterAsync(string firstName, string lastName, string email, string password)
    {
        if (_users.Any(u => u.Email == email))
            return Task.FromResult(new RegisterResult(false, "Email already registered."));

        var id = _users.Count + 1;
        _users.Add((id, firstName, lastName, email, HashPassword(password), "User"));
        return Task.FromResult(new RegisterResult(true, null));
    }

    public Task<UserDto?> GetByIdAsync(int id)
    {
        var u = _users.FirstOrDefault(x => x.Id == id);
        if (u == default) return Task.FromResult<UserDto?>(null);
        return Task.FromResult<UserDto?>(new UserDto(u.Id, u.FirstName, u.LastName, u.Email, u.Role, null));
    }

    public Task<UserDto?> GetByEmailAsync(string email)
    {
        var u = _users.FirstOrDefault(x => x.Email == email);
        if (u == default) return Task.FromResult<UserDto?>(null);
        return Task.FromResult<UserDto?>(new UserDto(u.Id, u.FirstName, u.LastName, u.Email, u.Role, null));
    }

    // NOTE: SHA256 with a static salt is used here for demo purposes only.
    // In production, use a proper adaptive hashing algorithm such as BCrypt or Argon2.
    private static string HashPassword(string password)
    {
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password + "ChatPortalSalt"));
        return Convert.ToHexString(bytes);
    }
}
