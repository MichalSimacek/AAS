using Microsoft.AspNetCore.Mvc;
namespace AAS.Web.Controllers
{
    public class ContactsController : Controller
    {
        public IActionResult Index() => View();
    }
}