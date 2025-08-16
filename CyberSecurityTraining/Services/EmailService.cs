namespace CyberSecurityTraining.Services
{
    public interface IEmailService
    {
        Task SendInvitationEmailAsync(string email, string invitationLink);
        Task SendPasswordResetEmailAsync(string email, string resetLink);
    }

    public class EmailService : IEmailService
    {
        private readonly ILogger<EmailService> _logger;

        public EmailService(ILogger<EmailService> logger)
        {
            _logger = logger;
        }

        public async Task SendInvitationEmailAsync(string email, string invitationLink)
        {
            // Mock email service implementation
            _logger.LogInformation($"Sending invitation email to {email} with link: {invitationLink}");
            
            // In a real implementation, you would use an email service like SendGrid, SMTP, etc.
            await Task.CompletedTask;
        }

        public async Task SendPasswordResetEmailAsync(string email, string resetLink)
        {
            // Mock email service implementation
            _logger.LogInformation($"Sending password reset email to {email} with link: {resetLink}");
            
            // In a real implementation, you would use an email service like SendGrid, SMTP, etc.
            await Task.CompletedTask;
        }
    }
}
