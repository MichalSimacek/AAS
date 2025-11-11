using AAS.Web.Models;
using MailKit.Net.Smtp;
using MimeKit;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace AAS.Web.Services
{
    public class EmailService
    {
        private readonly IConfiguration _cfg;
        public EmailService(IConfiguration cfg) { _cfg = cfg; }

        public async Task SendInquiryAsync(Inquiry i)
        {
            try
            {
                // Get email configuration from environment variables or config
                var to = Environment.GetEnvironmentVariable("EMAIL_TO") ?? _cfg["Email:To"];
                var from = Environment.GetEnvironmentVariable("EMAIL_FROM") ?? _cfg["Email:From"];
                var host = Environment.GetEnvironmentVariable("EMAIL_SMTP_HOST") ?? _cfg["Email:SmtpHost"];
                var port = int.Parse(Environment.GetEnvironmentVariable("EMAIL_SMTP_PORT") ?? _cfg["Email:SmtpPort"] ?? "587");
                var useStartTls = bool.Parse(Environment.GetEnvironmentVariable("EMAIL_USE_STARTTLS") ?? _cfg["Email:UseStartTls"] ?? "true");
                var user = Environment.GetEnvironmentVariable("EMAIL_USERNAME") ?? _cfg["Email:Username"];
                var pass = Environment.GetEnvironmentVariable("EMAIL_PASSWORD") ?? _cfg["Email:Password"];

                // Validate configuration
                if (string.IsNullOrWhiteSpace(to) || string.IsNullOrWhiteSpace(from) || string.IsNullOrWhiteSpace(host))
                {
                    // SECURITY: Log configuration issue without exposing details
                    Console.WriteLine("[SECURE] Email configuration incomplete");
                    throw new InvalidOperationException("Email service is not properly configured");
                }

                var msg = new MimeMessage();
                msg.From.Add(MailboxAddress.Parse(from));
                msg.To.Add(MailboxAddress.Parse(to));
                msg.Subject = $"Inquiry: {i.CollectionTitle ?? "General"}";

                var builder = new BodyBuilder { TextBody = "See attached inquiry PDF." };
                builder.Attachments.Add("inquiry.pdf", BuildPdf(i), new ContentType("application", "pdf"));
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
                
                Console.WriteLine($"[EMAIL] Connecting to SMTP {host}:{port} with security: {secureOptions}");
                
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                await client.ConnectAsync(host, port, secureOptions, cts.Token);
                Console.WriteLine($"[EMAIL] Connected successfully");
                
                if (!string.IsNullOrWhiteSpace(user) && !string.IsNullOrWhiteSpace(pass))
                {
                    Console.WriteLine($"[EMAIL] Authenticating with user: {user}");
                    await client.AuthenticateAsync(user, pass, cts.Token);
                    Console.WriteLine($"[EMAIL] Authenticated successfully");
                }
                
                Console.WriteLine($"[EMAIL] Sending email to {to}");
                await client.SendAsync(msg, cts.Token);
                Console.WriteLine($"[EMAIL] Email sent successfully");
                
                await client.DisconnectAsync(true, cts.Token);
            }
            catch (Exception ex)
            {
                // SECURITY: Log error type without exposing sensitive configuration
                Console.WriteLine($"[SECURE] Email sending failed: {ex.GetType().Name}");
                throw new InvalidOperationException("Failed to send email notification");
            }
        }

        private static byte[] BuildPdf(Inquiry i)
        {
            QuestPDF.Settings.License = LicenseType.Community;
            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(30);
                    page.Header().Text("Aristocratic Artwork Sale — Inquiry").SemiBold().FontSize(20);
                    page.Content().Table(t =>
                    {
                        t.ColumnsDefinition(c => { c.RelativeColumn(1); c.RelativeColumn(3); });
                        void Row(string k, string? v) { t.Cell().Text(k); t.Cell().Text(v ?? ""); }
                        Row("Collection", i.CollectionTitle);
                        Row("First name", i.FirstName);
                        Row("Last name", i.LastName);
                        Row("Email", i.Email);
                        Row("Phone", i.Phone);
                        Row("Message", i.Message);
                        Row("Created (UTC)", i.CreatedUtc.ToString("u"));
                        Row("Origin IP", i.OriginIp);
                    });
                });
            }).GeneratePdf();
        }
    }
}