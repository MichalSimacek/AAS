using Microsoft.AspNetCore.Mvc;

namespace AAS.Web.Controllers
{
    public class HowToController : Controller
    {
        // GET: /HowTo (Main page with both Buy and Sell info)
        public IActionResult Index()
        {
            return View();
        }

        // GET: /HowTo/Sell
        public IActionResult Sell()
        {
            return View();
        }

        // GET: /HowTo/Buy
        public IActionResult Buy()
        {
            return View();
        }

        // GET: /HowTo/AASVerified (Explanation of AAS Verified badge)
        public IActionResult AASVerified()
        {
            return View();
        }
    }
}
