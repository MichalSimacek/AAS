using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace AAS.Web.Data
{
    public class AdminSeeder : IHostedService
    {
        private readonly IServiceProvider _sp; private readonly ILogger<AdminSeeder> _log; private readonly IConfiguration _cfg;
        public AdminSeeder(IServiceProvider sp, ILogger<AdminSeeder> log, IConfiguration cfg) { _sp = sp; _log = log; _cfg = cfg; }
        public async Task StartAsync(CancellationToken ct)
        {
            using var scope = _sp.CreateScope();
            var userMgr = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
            var roleMgr = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            if (!await roleMgr.RoleExistsAsync("Admin")) await roleMgr.CreateAsync(new IdentityRole("Admin"));

            // Get admin credentials from environment variables first, fallback to config
            var email = Environment.GetEnvironmentVariable("ADMIN_EMAIL")
                        ?? _cfg["Admin:Email"];
            var pwd = Environment.GetEnvironmentVariable("ADMIN_PASSWORD")
                      ?? _cfg["Admin:Password"];

            // Validate that credentials are set
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(pwd))
            {
                _log.LogWarning("Admin account not created: ADMIN_EMAIL and ADMIN_PASSWORD must be set in environment variables or appsettings.json");
                return;
            }

            if (pwd.Length < 12)
            {
                _log.LogError("Admin password must be at least 12 characters long");
                return;
            }

            var user = await userMgr.FindByEmailAsync(email);
            if (user == null)
            {
                user = new IdentityUser { UserName = email, Email = email, EmailConfirmed = true };
                var res = await userMgr.CreateAsync(user, pwd);
                if (!res.Succeeded)
                {
                    _log.LogError("Admin creation failed: {err}", string.Join(",", res.Errors.Select(e => e.Description)));
                }
                else
                {
                    await userMgr.AddToRoleAsync(user, "Admin");
                    _log.LogInformation("Admin account created successfully: {email}", email);
                }
            }
        }
        public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
    }
}