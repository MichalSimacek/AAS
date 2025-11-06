using Microsoft.AspNetCore.Mvc;
namespace AAS.Web.Controllers
{
    public class AboutController : Controller
    {
        public IActionResult Index() => View();
    }
}