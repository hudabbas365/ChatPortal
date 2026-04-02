namespace ChatPortal.Services;

public interface IEmailService
{
    Task SendEmailAsync(string to, string subject, string htmlBody);
    Task SendWelcomeEmailAsync(string to, string firstName);
    Task SendPasswordResetEmailAsync(string to, string resetLink);
}
