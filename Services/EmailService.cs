namespace ChatPortal.Services;

public class EmailService : IEmailService
{
    private readonly ILogger<EmailService> _logger;

    public EmailService(ILogger<EmailService> logger)
    {
        _logger = logger;
    }

    public Task SendEmailAsync(string to, string subject, string htmlBody)
    {
        _logger.LogInformation("Email to {To} | Subject: {Subject}", to, subject);
        return Task.CompletedTask;
    }

    public Task SendWelcomeEmailAsync(string to, string firstName)
    {
        _logger.LogInformation("Welcome email to {To} ({FirstName})", to, firstName);
        return Task.CompletedTask;
    }

    public Task SendPasswordResetEmailAsync(string to, string resetLink)
    {
        _logger.LogInformation("Password reset email to {To}", to);
        return Task.CompletedTask;
    }
}
