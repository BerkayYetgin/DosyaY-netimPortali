using Microsoft.AspNetCore.Mvc;

namespace dosyayonetim.ui.Controllers
{
    public class AdminController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Users()
        {
            return View();
        }

        public IActionResult Storage()
        {
            return View();
        }
    }
}
