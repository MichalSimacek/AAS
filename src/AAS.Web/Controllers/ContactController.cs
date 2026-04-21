using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Caching.Memory;
using MailKit.Net.Smtp;
using MimeKit;

namespace AAS.Web.Controllers
{
    [EnableRateLimiting("contact")]
    public class ContactsController : Controller
    {
        private readonly IConfiguration _cfg;
        private readonly IMemoryCache _cache;

        public ContactsController(IConfiguration cfg, IMemoryCache cache)
        {
            _cfg = cfg;
            _cache = cache;
        }

        public IActionResult Index() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Submit(string name, string email, string? subject, string message)
        {
            // SECURITY: Enforce input length limits (defense against DoS and abuse)
            const int maxNameLen = 120;
            const int maxEmailLen = 254; // RFC 5321
            const int maxSubjectLen = 200;
            const int maxMessageLen = 5000;

            // Basic validation
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(message))
            {
                TempData["Error"] = "Please fill in all required fields.";
                return RedirectToAction("Index");
            }

            if (name.Length > maxNameLen || email.Length > maxEmailLen ||
                (subject?.Length ?? 0) > maxSubjectLen || message.Length > maxMessageLen)
            {
                TempData["Error"] = "Your input exceeds the allowed length.";
                return RedirectToAction("Index");
            }

            // SECURITY: Strip control characters and CR/LF (SMTP header injection defense)
            name = System.Text.RegularExpressions.Regex.Replace(name, @"[\x00-\x1F\x7F]", " ").Trim();
            email = email.Trim();
            subject = subject == null ? null : System.Text.RegularExpressions.Regex.Replace(subject, @"[\x00-\x1F\x7F]", " ").Trim();

            // SECURITY: Strict email format validation via System.Net.Mail
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                if (addr.Address != email) throw new FormatException();
            }
            catch
            {
                TempData["Error"] = "Please enter a valid email address.";
                return RedirectToAction("Index");
            }

            // Security: Get real IP
            var forwardedFor = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            var remoteIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            string ip = remoteIp;
            if (!string.IsNullOrEmpty(forwardedFor))
            {
                var ips = forwardedFor.Split(',');
                var firstIp = ips[0].Trim();
                if (System.Net.IPAddress.TryParse(firstIp, out _))
                {
                    ip = firstIp;
                }
            }

            // Rate limiting - 5 requests per 15 minutes per IP
            var key = $"contact:{ip}";
            var count = _cache.GetOrCreate<int>(key, e => { e.SlidingExpiration = TimeSpan.FromMinutes(15); return 0; });
            if (count >= 5)
            {
                TempData["Error"] = "Too many messages. Please try again later.";
                return RedirectToAction("Index");
            }

            try
            {
                await SendContactEmailAsync(name, email, subject, message, ip);
                _cache.Set(key, count + 1, new MemoryCacheEntryOptions { SlidingExpiration = TimeSpan.FromMinutes(15) });
                TempData["Success"] = "Your message has been sent successfully. We'll get back to you within 24 hours.";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CONTACT] Email sending failed: {ex.GetType().Name} - {ex.Message}");
                TempData["Error"] = "Failed to send message. Please try again or email us directly.";
            }

            return RedirectToAction("Index");
        }

        private async Task SendContactEmailAsync(string name, string email, string? subject, string message, string ip)
        {
            var to = Environment.GetEnvironmentVariable("EMAIL_TO") ?? _cfg["Email:To"] ?? "info@aristocraticartworksale.com";
            var from = Environment.GetEnvironmentVariable("EMAIL_FROM") ?? _cfg["Email:From"];
            var host = Environment.GetEnvironmentVariable("EMAIL_SMTP_HOST") ?? _cfg["Email:SmtpHost"];
            var port = int.Parse(Environment.GetEnvironmentVariable("EMAIL_SMTP_PORT") ?? _cfg["Email:SmtpPort"] ?? "587");
            var useStartTls = bool.Parse(Environment.GetEnvironmentVariable("EMAIL_USE_STARTTLS") ?? _cfg["Email:UseStartTls"] ?? "true");
            var user = Environment.GetEnvironmentVariable("EMAIL_USERNAME") ?? _cfg["Email:Username"];
            var pass = Environment.GetEnvironmentVariable("EMAIL_PASSWORD") ?? _cfg["Email:Password"];

            if (string.IsNullOrWhiteSpace(from) || string.IsNullOrWhiteSpace(host))
            {
                Console.WriteLine("[CONTACT] Email configuration incomplete - using fallback notification");
                throw new InvalidOperationException("Email service is not properly configured");
            }

            var msg = new MimeMessage();
            msg.From.Add(MailboxAddress.Parse(from));
            msg.To.Add(MailboxAddress.Parse(to));
            msg.ReplyTo.Add(new MailboxAddress(name, email));
            msg.Subject = $"Contact Form: {subject ?? "General Inquiry"}";

            var bodyBuilder = new BodyBuilder();
            bodyBuilder.HtmlBody = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #240046, #4B0082); color: #D4AF37; padding: 20px; text-align: center; }}
        .content {{ background: #f9f9f9; padding: 30px; }}
        .field {{ margin-bottom: 20px; }}
        .label {{ font-weight: bold; color: #4B0082; }}
        .value {{ margin-top: 5px; }}
        .footer {{ background: #240046; color: #9ca3af; padding: 15px; text-align: center; font-size: 12px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1 style='margin:0;'>New Contact Message</h1>
            <p style='margin:10px 0 0 0;'>Aristocratic Artwork Sale</p>
        </div>
        <div class='content'>
            <div class='field'>
                <div class='label'>Name:</div>
                <div class='value'>{System.Net.WebUtility.HtmlEncode(name)}</div>
            </div>
            <div class='field'>
                <div class='label'>Email:</div>
                <div class='value'><a href='mailto:{System.Net.WebUtility.HtmlEncode(email)}'>{System.Net.WebUtility.HtmlEncode(email)}</a></div>
            </div>
            <div class='field'>
                <div class='label'>Subject:</div>
                <div class='value'>{System.Net.WebUtility.HtmlEncode(subject ?? "General Inquiry")}</div>
            </div>
            <div class='field'>
                <div class='label'>Message:</div>
                <div class='value'>{System.Net.WebUtility.HtmlEncode(message).Replace("\n", "<br/>")}</div>
            </div>
        </div>
        <div class='footer'>
            <p>Received: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC</p>
            <p>IP: {ip}</p>
        </div>
    </div>
</body>
</html>";

            bodyBuilder.TextBody = $@"
New Contact Message - Aristocratic Artwork Sale
================================================

Name: {name}
Email: {email}
Subject: {subject ?? "General Inquiry"}

Message:
{message}

---
Received: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC
IP: {ip}
";

            msg.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();

            var secureOptions = MailKit.Security.SecureSocketOptions.None;
            if (port == 465)
            {
                secureOptions = MailKit.Security.SecureSocketOptions.SslOnConnect;
            }
            else if (useStartTls && port != 1025)
            {
                secureOptions = MailKit.Security.SecureSocketOptions.StartTls;
            }

            Console.WriteLine($"[CONTACT] Connecting to SMTP {host}:{port}");
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            await client.ConnectAsync(host, port, secureOptions, cts.Token);

            if (!string.IsNullOrWhiteSpace(user) && !string.IsNullOrWhiteSpace(pass))
            {
                await client.AuthenticateAsync(user, pass, cts.Token);
            }

            await client.SendAsync(msg, cts.Token);
            Console.WriteLine($"[CONTACT] Email sent successfully to {to}");
            await client.DisconnectAsync(true, cts.Token);
        }
    }
}
