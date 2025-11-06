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
            // Get email configuration from environment variables or config
            var to = Environment.GetEnvironmentVariable("EMAIL_TO") ?? _cfg["Email:To"];
            var from = Environment.GetEnvironmentVariable("EMAIL_FROM") ?? _cfg["Email:From"];
            var host = Environment.GetEnvironmentVariable("SMTP_HOST") ?? _cfg["Email:SmtpHost"];
            var port = int.Parse(Environment.GetEnvironmentVariable("SMTP_PORT") ?? _cfg["Email:SmtpPort"] ?? "587");
            var useStartTls = bool.Parse(Environment.GetEnvironmentVariable("SMTP_USE_STARTTLS") ?? _cfg["Email:UseStartTls"] ?? "true");
            var user = Environment.GetEnvironmentVariable("SMTP_USERNAME") ?? _cfg["Email:Username"];
            var pass = Environment.GetEnvironmentVariable("SMTP_PASSWORD") ?? _cfg["Email:Password"];

            // Validate configuration
            if (string.IsNullOrWhiteSpace(to) || string.IsNullOrWhiteSpace(from) || string.IsNullOrWhiteSpace(host))
            {
                throw new InvalidOperationException("Email configuration is incomplete. Set SMTP_HOST, EMAIL_FROM, and EMAIL_TO environment variables.");
            }

            var msg = new MimeMessage();
            msg.From.Add(MailboxAddress.Parse(from));
            msg.To.Add(MailboxAddress.Parse(to));
            msg.Subject = $"Inquiry: {i.CollectionTitle ?? "General"}";

            var builder = new BodyBuilder { TextBody = "See attached inquiry PDF." };
            builder.Attachments.Add("inquiry.pdf", BuildPdf(i), new ContentType("application", "pdf"));
            msg.Body = builder.ToMessageBody();

            using var client = new SmtpClient();
            await client.ConnectAsync(host, port, useStartTls ? MailKit.Security.SecureSocketOptions.StartTls : MailKit.Security.SecureSocketOptions.Auto);
            if (!string.IsNullOrWhiteSpace(user) && !string.IsNullOrWhiteSpace(pass))
            {
                await client.AuthenticateAsync(user, pass);
            }
            await client.SendAsync(msg);
            await client.DisconnectAsync(true);
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