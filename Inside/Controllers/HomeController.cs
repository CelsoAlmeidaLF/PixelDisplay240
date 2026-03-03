using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PixelDisplay240Api.Controllers
{
    public class HomeController : Controller
    {
        /// <summary>
        /// Página principal - Design sempre acessível
        /// Protótipo requer autenticação (verificado pelo servidor via Cookie)
        /// </summary>
        [AllowAnonymous]
        public IActionResult Index()
        {
            ViewData["IsAuthenticated"] = User.Identity?.IsAuthenticated == true;
            ViewData["Username"] = User.Identity?.Name;
            ViewData["ActiveMode"] = "design";
            return View();
        }

        /// <summary>
        /// Rota para o modo Protótipo - REQUER AUTENTICAÇÃO
        /// Usa [Authorize] simples para garantir redirecionamento correto para Login
        /// Após login, retorna para a página principal com a aba de Protótipo ativa
        /// </summary>
        [Authorize]
        public IActionResult Prototype()
        {
            ViewData["IsAuthenticated"] = true;
            ViewData["Username"] = User.Identity?.Name;
            ViewData["ActiveMode"] = "prototype";
            return View("Index");
        }
    }
}
