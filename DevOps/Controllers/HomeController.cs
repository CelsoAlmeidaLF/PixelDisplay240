using Microsoft.AspNetCore.Mvc;

namespace PixelDisplay240Api.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
