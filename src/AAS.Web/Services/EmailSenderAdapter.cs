using MailKit.Net.Smtp;
using Microsoft.AspNetCore.Identity.UI.Services;
using MimeKit;

namespace AAS.Web.Services
{
    public class EmailSenderAdapter : IEmailSender
    {
        private readonly IConfiguration _cfg;
        private readonly ILogger<EmailSenderAdapter> _logger;

        public EmailSenderAdapter(IConfiguration cfg, ILogger<EmailSenderAdapter> logger)
        {
            _cfg = cfg;
            _logger = logger;
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            try
            {
                // Get email configuration from environment variables or config
                var from = Environment.GetEnvironmentVariable("EMAIL_FROM") ?? _cfg["Email:From"] ?? "noreply@aristocraticartworksale.com";
                var host = Environment.GetEnvironmentVariable("EMAIL_SMTP_HOST") ?? _cfg["Email:SmtpHost"];
                var port = int.Parse(Environment.GetEnvironmentVariable("EMAIL_SMTP_PORT") ?? _cfg["Email:SmtpPort"] ?? "587");
                var useStartTls = bool.Parse(Environment.GetEnvironmentVariable("EMAIL_USE_STARTTLS") ?? _cfg["Email:UseStartTls"] ?? "true");
                var user = Environment.GetEnvironmentVariable("EMAIL_USERNAME") ?? _cfg["Email:Username"];
                var pass = Environment.GetEnvironmentVariable("EMAIL_PASSWORD") ?? _cfg["Email:Password"];

                // For development: If SMTP not configured, just log the email
                if (string.IsNullOrWhiteSpace(host))
                {
                    _logger.LogWarning("SMTP not configured. Email would be sent to {Email} with subject: {Subject}", email, subject);
                    _logger.LogInformation("Email content: {Content}", htmlMessage);
                    Console.WriteLine($"\n\n=== EMAIL (Development Mode) ===");
                    Console.WriteLine($"To: {email}");
                    Console.WriteLine($"Subject: {subject}");
                    Console.WriteLine($"Body:\n{htmlMessage}");
                    Console.WriteLine($"===================================\n\n");
                    return;
                }

                var msg = new MimeMessage();
                msg.From.Add(MailboxAddress.Parse(from));
                msg.To.Add(MailboxAddress.Parse(email));
                msg.Subject = subject;

                var builder = new BodyBuilder { HtmlBody = htmlMessage };
                msg.Body = builder.ToMessageBody();

                using var client = new SmtpClient();
                
                // Determine SSL/TLS options based on port and configuration
                var secureOptions = MailKit.Security.SecureSocketOptions.None;
                if (port == 465)
                {
                    secureOptions = MailKit.Security.SecureSocketOptions.SslOnConnect;
                }
                else if (useStartTls && port != 1025)
                {
                    secureOptions = MailKit.Security.SecureSocketOptions.StartTls;
                }
                
                _logger.LogInformation("Connecting to SMTP server {Host}:{Port} with security: {Security}", host, port, secureOptions);
                
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                await client.ConnectAsync(host, port, secureOptions, cts.Token);
                _logger.LogInformation("Connected successfully");
                
                if (!string.IsNullOrWhiteSpace(user) && !string.IsNullOrWhiteSpace(pass))
                {
                    _logger.LogInformation("Authenticating with user: {User}", user);
                    await client.AuthenticateAsync(user, pass, cts.Token);
                    _logger.LogInformation("Authenticated successfully");
                }
                
                _logger.LogInformation("Sending email to {Email}", email);
                await client.SendAsync(msg, cts.Token);
                _logger.LogInformation("Email sent successfully");
                
                await client.DisconnectAsync(true, cts.Token);
                
                _logger.LogInformation("Email sent successfully to {Email}", email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {Email}", email);
                // In production, you might want to throw or handle this differently
                throw;
            }
        }
    }
}
