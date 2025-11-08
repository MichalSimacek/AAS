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

            return LocalRedirect(returnUrl);
        }
    }
}