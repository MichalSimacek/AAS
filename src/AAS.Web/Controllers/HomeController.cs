using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;

namespace AAS.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly IConfiguration _configuration;

        public HomeController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public IActionResult Index() => View();

        public IActionResult Privacy() => View();

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SetLanguage(string culture, string returnUrl = "/")
        {
            // SECURITY: Validate culture against allowed list
            var allowedCultures = _configuration.GetSection("Localization:SupportedCultures")
                .Get<string[]>() ?? new[] { "en", "cs", "ru", "de", "es", "fr", "zh", "pt", "hi", "ja" };

            if (!allowedCultures.Contains(culture))
            {
                culture = "en"; // Default to English if invalid
            }

            // SECURITY: Validate returnUrl to prevent open redirects
            if (!Url.IsLocalUrl(returnUrl))
            {
                returnUrl = "/";
            }

            // SECURITY: Set secure cookie options
            Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
                new CookieOptions
                {
                    Expires = DateTimeOffset.UtcNow.AddYears(1),
                    HttpOnly = true,
                    SameSite = SameSiteMode.Lax,
                    Secure = true // Requires HTTPS
                }
            );

            // Variant C URL strategy: re-prefix returnUrl with the selected culture.
            // EN is the default and uses no prefix; all other languages use /{culture}/...
            var queryIdx = returnUrl.IndexOf('?');
            var path = queryIdx >= 0 ? returnUrl.Substring(0, queryIdx) : returnUrl;
            var query = queryIdx >= 0 ? returnUrl.Substring(queryIdx) : string.Empty;

            // Strip an existing leading culture segment from the path (if any).
            var firstSeg = path.TrimStart('/').Split('/', 2)[0];
            var nonDefaultCultures = new[] { "cs", "ru", "de", "es", "fr", "zh", "pt", "hi", "ja" };
            if (nonDefaultCultures.Contains(firstSeg))
            {
                path = path.Length > firstSeg.Length + 1 ? path.Substring(firstSeg.Length + 1) : "/";
            }

            if (string.IsNullOrEmpty(path)) path = "/";
            var newPath = culture == "en"
                ? path
                : $"/{culture}" + (path == "/" ? "" : path);
            if (string.IsNullOrEmpty(newPath)) newPath = "/";

            return LocalRedirect(newPath + query);
        }

    }
}
