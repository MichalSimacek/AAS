using System.Globalization;
using AAS.Web.Data;
using AAS.Web.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;
var config = builder.Configuration;

// Build connection string from environment variables or config
var connectionString = Environment.GetEnvironmentVariable("DATABASE_URL");
if (string.IsNullOrEmpty(connectionString))
{
    var host = Environment.GetEnvironmentVariable("DB_HOST") ?? "localhost";
    var port = Environment.GetEnvironmentVariable("DB_PORT") ?? "5432";
    var db = Environment.GetEnvironmentVariable("DB_NAME") ?? "aas";
    var user = Environment.GetEnvironmentVariable("DB_USER") ?? "aas";
    var pwd = Environment.GetEnvironmentVariable("DB_PASSWORD");

    if (string.IsNullOrEmpty(pwd))
    {
        connectionString = config.GetConnectionString("DefaultConnection");
    }
    else
    {
        connectionString = $"Host={host};Port={port};Database={db};Username={user};Password={pwd};Pooling=true;Keepalive=30;Maximum Pool Size=100;";
    }
}

if (string.IsNullOrEmpty(connectionString))
{
    throw new InvalidOperationException("Database connection string is not configured. Set DB_PASSWORD environment variable or ConnectionStrings:DefaultConnection in appsettings.json");
}

// PostgreSQL with connection resiliency and performance optimizations
services.AddDbContext<AppDbContext>(options =>
{
    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        // CRITICAL: Enable connection resiliency (retry on transient failures)
        npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(5),
            errorCodesToAdd: null);

        // PERFORMANCE: Set command timeout
        npgsqlOptions.CommandTimeout(30);
    })
    // PERFORMANCE: Disable tracking by default (enable when needed)
    .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
});

// Identity
services.AddDefaultIdentity<IdentityUser>(o =>
{
    o.SignIn.RequireConfirmedAccount = true; // Require email confirmation
    o.Password.RequiredLength = 12;
    o.Password.RequireNonAlphanumeric = true;
    o.Password.RequireUppercase = true;
    o.User.RequireUniqueEmail = true; // Ensure unique emails
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<AppDbContext>();

// MVC + Localization
services.AddLocalization();
services.AddControllersWithViews().AddViewLocalization();
services.AddRazorPages();

// Services
services.AddScoped<SlugService>();
services.AddScoped<ImageService>();
services.AddScoped<EmailService>();
services.AddTransient<Microsoft.AspNetCore.Identity.UI.Services.IEmailSender, EmailSenderAdapter>();

// TranslationService with HttpClient - CRITICAL: Must be singleton to reuse HttpClient
// But uses IServiceProvider to create scoped DbContext to avoid memory leak
services.AddHttpClient<TranslationService>();
services.AddSingleton<TranslationService>();

services.AddHostedService<AdminSeeder>();

// Response caching + compression
services.AddMemoryCache();
services.AddResponseCompression();
services.AddResponseCaching();

var app = builder.Build();

// Use fully qualified name to avoid collision with AAS.Web.Data.AppDbContext
System.AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

// CRITICAL FIX: Migrate DB asynchronously to avoid blocking startup
// Use async/await to prevent deadlocks
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
}

// Security
if (app.Environment.IsProduction())
{
    app.UseHsts();
}
app.UseHttpsRedirection();

app.Use((ctx, next) =>
{
    // Security Headers
    ctx.Response.Headers["X-Content-Type-Options"] = "nosniff";
    ctx.Response.Headers["X-Frame-Options"] = "DENY";
    ctx.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    ctx.Response.Headers["X-XSS-Protection"] = "0"; // Disabled as modern browsers use CSP
    ctx.Response.Headers["Permissions-Policy"] = "geolocation=(), microphone=(), camera=(), usb=()";

    // CSP - allow specific trusted sources and inline scripts for functionality
    var csp = "default-src 'self'; " +
              "script-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net; " +
              "style-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net https://fonts.googleapis.com; " +
              "img-src 'self' data: https:; " +
              "font-src 'self' data: https://fonts.gstatic.com; " +
              "media-src 'self'; " +
              "connect-src 'self' https://cdn.jsdelivr.net; " +
              "frame-ancestors 'none'; " +
              "base-uri 'self'; " +
              "form-action 'self'";
    ctx.Response.Headers["Content-Security-Policy"] = csp;

    // Remove server header
    ctx.Response.Headers.Remove("Server");
    ctx.Response.Headers.Remove("X-Powered-By");

    return next();
});

// Localization
var supported = config.GetSection("Localization:SupportedCultures").Get<string[]>() ?? new[] { "en" };
var defaultCulture = config["Localization:DefaultCulture"] ?? "en";
var cultures = Array.ConvertAll(supported, s => new CultureInfo(s));
var locOpts = new RequestLocalizationOptions
{
    DefaultRequestCulture = new Microsoft.AspNetCore.Localization.RequestCulture(defaultCulture),
    SupportedCultures = cultures,
    SupportedUICultures = cultures
};
locOpts.RequestCultureProviders.Insert(0, new Microsoft.AspNetCore.Localization.CookieRequestCultureProvider());
app.UseRequestLocalization(locOpts);

app.UseStaticFiles();
app.UseResponseCompression();
app.UseResponseCaching();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Collections}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();
app.Run();