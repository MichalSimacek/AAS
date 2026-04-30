using System.Globalization;
using System.Net;
using System.Threading.RateLimiting;
using AAS.Web.Data;
using AAS.Web.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;
var config = builder.Configuration;

// Configure Data Protection to persist keys across container restarts
var dataProtectionPath = Path.Combine(Directory.GetCurrentDirectory(), "DataProtection-Keys");
Directory.CreateDirectory(dataProtectionPath);
services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionPath))
    .SetApplicationName("AAS.Web");

// Configure forwarded headers for reverse proxy support (Nginx)
services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

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

// Identity with comprehensive security settings
services.AddDefaultIdentity<IdentityUser>(o =>
{
    // Account settings
    o.SignIn.RequireConfirmedAccount = true; // Require email confirmation
    o.User.RequireUniqueEmail = true; // Ensure unique emails
    
    // Password policy
    o.Password.RequiredLength = 12;
    o.Password.RequireNonAlphanumeric = true;
    o.Password.RequireUppercase = true;
    o.Password.RequireLowercase = true;
    o.Password.RequireDigit = true;
    
    // SECURITY: Account lockout to prevent brute force attacks
    o.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    o.Lockout.MaxFailedAccessAttempts = 5;
    o.Lockout.AllowedForNewUsers = true;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<AppDbContext>();

// MVC + Localization
services.AddLocalization(options => options.ResourcesPath = "Resources");
services.AddControllersWithViews().AddViewLocalization().AddRazorRuntimeCompilation();
services.AddRazorPages().AddRazorRuntimeCompilation();

// SECURITY: Configure anti-forgery to accept tokens from headers (for AJAX requests)
services.AddAntiforgery(options =>
{
    options.HeaderName = "RequestVerificationToken";
});

// Services
services.AddScoped<SlugService>();
services.AddScoped<ImageService>();
services.AddScoped<EmailService>();
services.AddTransient<Microsoft.AspNetCore.Identity.UI.Services.IEmailSender, EmailSenderAdapter>();

// TranslationService with HttpClient - CRITICAL: Must be singleton to reuse HttpClient
// But uses IServiceProvider to create scoped DbContext to avoid memory leak
services.AddHttpClient<TranslationService>();
services.AddSingleton<TranslationService>();

// DeepL Translation Service - Singleton to match TranslationService lifecycle
services.AddHttpClient();
services.AddSingleton<IDeepLService, DeepLService>();

services.AddHostedService<AdminSeeder>();

// Response caching + compression
services.AddMemoryCache();
services.AddResponseCompression();
services.AddResponseCaching();

// SECURITY: Rate limiting to mitigate brute force / DoS attacks
// - "global": soft limit on all requests per IP (generous to avoid blocking normal browsing)
// - "api": stricter limit for API endpoints
// - "auth": very strict for login / register / password reset
// - "contact": strict for public contact form
services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = (int)HttpStatusCode.TooManyRequests;

    string GetClientIp(HttpContext ctx)
    {
        var forwardedFor = ctx.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            var firstIp = forwardedFor.Split(',')[0].Trim();
            if (IPAddress.TryParse(firstIp, out _))
                return firstIp;
        }
        return ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    // Global partition: 300 req / minute per IP (generous default)
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(ctx =>
    {
        // Do not rate limit static asset / health routes
        var path = ctx.Request.Path.Value ?? string.Empty;
        if (path.StartsWith("/uploads", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/css", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/js", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/lib", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/images", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/favicon", StringComparison.OrdinalIgnoreCase))
        {
            return RateLimitPartition.GetNoLimiter<string>("static");
        }

        return RateLimitPartition.GetFixedWindowLimiter(GetClientIp(ctx), _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 300,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            AutoReplenishment = true
        });
    });

    // Named policy: API endpoints (60 req / minute / IP)
    options.AddPolicy("api", ctx =>
        RateLimitPartition.GetFixedWindowLimiter(GetClientIp(ctx), _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 60,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            AutoReplenishment = true
        }));

    // Named policy: Authentication endpoints (10 req / 15 min / IP)
    options.AddPolicy("auth", ctx =>
        RateLimitPartition.GetFixedWindowLimiter(GetClientIp(ctx), _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 10,
            Window = TimeSpan.FromMinutes(15),
            QueueLimit = 0,
            AutoReplenishment = true
        }));

    // Named policy: Contact form (5 req / 15 min / IP)
    options.AddPolicy("contact", ctx =>
        RateLimitPartition.GetFixedWindowLimiter(GetClientIp(ctx), _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 5,
            Window = TimeSpan.FromMinutes(15),
            QueueLimit = 0,
            AutoReplenishment = true
        }));

    // Named policy: Comments (20 req / 10 min / IP)
    options.AddPolicy("comments", ctx =>
        RateLimitPartition.GetFixedWindowLimiter(GetClientIp(ctx), _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 20,
            Window = TimeSpan.FromMinutes(10),
            QueueLimit = 0,
            AutoReplenishment = true
        }));
});

// SECURITY: HTML sanitizer registration for user-generated HTML (blog/collections)
services.AddSingleton<Ganss.Xss.IHtmlSanitizer>(_ =>
{
    var s = new Ganss.Xss.HtmlSanitizer();
    // TinyMCE may use these extras; add common safe ones
    s.AllowedSchemes.Add("mailto");
    s.AllowedSchemes.Add("tel");
    s.AllowedAttributes.Add("class");
    s.AllowedAttributes.Add("id");
    s.AllowedAttributes.Add("style");
    s.AllowedAttributes.Add("target");
    s.AllowedAttributes.Add("rel");
    s.AllowedCssProperties.Add("text-align");
    s.AllowedCssProperties.Add("font-weight");
    s.AllowedCssProperties.Add("font-style");
    s.AllowedCssProperties.Add("text-decoration");
    s.AllowedCssProperties.Add("color");
    s.AllowedCssProperties.Add("background-color");
    return s;
});

// SECURITY: Identity application cookie hardening (HttpOnly, Secure, SameSite)
services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.ExpireTimeSpan = TimeSpan.FromHours(2);
    options.SlidingExpiration = true;
    options.LoginPath = "/Identity/Account/Login";
    options.LogoutPath = "/Identity/Account/Logout";
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";
});

// SECURITY: Limit request body size globally to prevent DoS via huge uploads
services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(o =>
{
    o.MultipartBodyLengthLimit = 100L * 1024 * 1024; // 100 MB (gallery can include several images)
    o.ValueCountLimit = 1024;
});

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

// CRITICAL: Configure forwarded headers BEFORE other middleware
app.UseForwardedHeaders();

// SECURITY: Exception handling middleware
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

// Only redirect to HTTPS if running on HTTPS port
if (app.Environment.IsProduction() && app.Urls.Any(u => u.Contains("https")))
{
    app.UseHttpsRedirection();
}

app.Use((ctx, next) =>
{
    // SECURITY HEADERS - Comprehensive protection against common web vulnerabilities
    ctx.Response.Headers["X-Content-Type-Options"] = "nosniff";
    ctx.Response.Headers["X-Frame-Options"] = "DENY";
    ctx.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    ctx.Response.Headers["X-XSS-Protection"] = "0"; // Disabled as modern browsers use CSP
    ctx.Response.Headers["X-Download-Options"] = "noopen";
    ctx.Response.Headers["Permissions-Policy"] = "geolocation=(), microphone=(), camera=(), usb=(), payment=()";
    
    // HSTS - Force HTTPS for 1 year (only in production)
    if (!app.Environment.IsDevelopment())
    {
        ctx.Response.Headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains; preload";
    }

    // CSP - Content Security Policy with Google Analytics and TinyMCE support
    // Note: 'unsafe-inline' is needed for Bootstrap and inline event handlers
    // Google Analytics is loaded dynamically only after user consent (GDPR compliant)
    // TinyMCE CDN included for blog editor functionality
    // Consider migrating to nonce-based CSP in future for better security
    var csp = "default-src 'self'; " +
              "script-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net https://www.googletagmanager.com https://cdn.tiny.cloud; " +
              "style-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net https://fonts.googleapis.com; " +
              "img-src 'self' data: https: blob:; " +
              "font-src 'self' data: https://fonts.gstatic.com https://cdn.jsdelivr.net; " +
              "media-src 'self' blob:; " +
              "connect-src 'self' https://cdn.jsdelivr.net https://www.google-analytics.com https://analytics.google.com https://www.googletagmanager.com https://*.google-analytics.com; " +
              "frame-ancestors 'none'; " +
              "base-uri 'self'; " +
              "form-action 'self'";
    
    // Only add upgrade-insecure-requests if running on HTTPS
    if (ctx.Request.IsHttps)
    {
        csp += "; upgrade-insecure-requests";
    }
    
    ctx.Response.Headers["Content-Security-Policy"] = csp;

    // Remove server identification headers
    ctx.Response.Headers.Remove("Server");
    ctx.Response.Headers.Remove("X-Powered-By");
    ctx.Response.Headers.Remove("X-AspNet-Version");
    ctx.Response.Headers.Remove("X-AspNetMvc-Version");

    return next();
});

// Localization - configure culture detection in this order:
// 1. Cookie (user's explicit choice via language selector)
// 2. Accept-Language header (browser's default language)
// 3. Default culture (fallback to English)
var supported = config.GetSection("Localization:SupportedCultures").Get<string[]>() ?? new[] { "en" };
var defaultCulture = config["Localization:DefaultCulture"] ?? "en";
var cultures = Array.ConvertAll(supported, s => new CultureInfo(s));
var locOpts = new RequestLocalizationOptions
{
    DefaultRequestCulture = new Microsoft.AspNetCore.Localization.RequestCulture(defaultCulture),
    SupportedCultures = cultures,
    SupportedUICultures = cultures
};
// Clear default providers and set custom order
locOpts.RequestCultureProviders.Clear();
locOpts.RequestCultureProviders.Add(new Microsoft.AspNetCore.Localization.CookieRequestCultureProvider());
locOpts.RequestCultureProviders.Add(new Microsoft.AspNetCore.Localization.AcceptLanguageHeaderRequestCultureProvider());
app.UseRequestLocalization(locOpts);

app.UseStaticFiles();
app.UseResponseCompression();
app.UseResponseCaching();

app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

// Area routes
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

// 301 redirect for legacy lowercase /collections/{slug} URLs that were indexed
// by Google before the canonical /Collections/Details/{slug} pattern was set.
// This preserves SEO link juice and fixes 404s reported in Search Console.
app.MapGet("/collections/{slug}", (string slug) =>
    Results.RedirectToRoute("default",
        new { controller = "Collections", action = "Details", id = slug },
        permanent: true));

// Default route
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

app.Run();