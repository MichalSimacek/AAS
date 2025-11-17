using Microsoft.AspNetCore.Mvc;

namespace AAS.Web.Controllers
{
    public class HowToController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
