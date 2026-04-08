using System.Security.Cryptography;
using System.Text;
using ChatPortal.Data;
using ChatPortal.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace ChatPortal.Services;

public class UserService : IUserService
{
    private readonly AppDbContext _db;
    private readonly IJwtService _jwtService;

    public UserService(AppDbContext db, IJwtService jwtService)
    {
        _db = db;
        _jwtService = jwtService;
    }

    public async Task<LoginResult> LoginAsync(string email, string password)
    {
        var hash = HashPassword(password);
        var user = await _db.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Email == email && u.PasswordHash == hash);

        if (user == null)
            return new LoginResult(false, null, null, "Invalid email or password.");

        var accessToken = _jwtService.GenerateAccessToken(user.Id, user.Email, user.Role.Name);
        var refreshToken = _jwtService.GenerateRefreshToken();
        return new LoginResult(true, accessToken, refreshToken, null);
    }

    public async Task<RegisterResult> RegisterAsync(string firstName, string lastName, string email, string password)
    {
        if (await _db.Users.AnyAsync(u => u.Email == email))
            return new RegisterResult(false, "Email already registered.");

        var userRole = await _db.Roles.FirstOrDefaultAsync(r => r.Name == "User");
        if (userRole == null)
            return new RegisterResult(false, "Default role not found. Please contact support.");

        var user = new User
        {
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            PasswordHash = HashPassword(password),
            RoleId = userRole.Id
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        return new RegisterResult(true, null);
    }

    public async Task<UserDto?> GetByIdAsync(Guid id)
    {
        var u = await _db.Users.Include(x => x.Role).FirstOrDefaultAsync(x => x.Id == id);
        if (u == null) return null;
        return new UserDto(u.Id, u.FirstName, u.LastName, u.Email, u.Role.Name, u.AvatarUrl);
    }

    public async Task<UserDto?> GetByEmailAsync(string email)
    {
        var u = await _db.Users.Include(x => x.Role).FirstOrDefaultAsync(x => x.Email == email);
        if (u == null) return null;
        return new UserDto(u.Id, u.FirstName, u.LastName, u.Email, u.Role.Name, u.AvatarUrl);
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
