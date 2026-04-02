namespace ChatPortal.Services;

public record UserDto(int Id, string FirstName, string LastName, string Email, string Role, string? AvatarUrl);
public record LoginResult(bool Success, string? AccessToken, string? RefreshToken, string? Error);
public record RegisterResult(bool Success, string? Error);

public interface IUserService
{
    Task<LoginResult> LoginAsync(string email, string password);
    Task<RegisterResult> RegisterAsync(string firstName, string lastName, string email, string password);
    Task<UserDto?> GetByIdAsync(int id);
    Task<UserDto?> GetByEmailAsync(string email);
}
