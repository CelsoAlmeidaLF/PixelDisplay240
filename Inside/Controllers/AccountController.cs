using Microsoft.AspNetCore.Mvc;

namespace PixelDisplay240Api.Controllers;

public class AccountController : Controller
{
    public IActionResult Login(string? returnUrl = null)
    {
        // Se já estiver autenticado, redireciona para home
        if (HttpContext.Request.Cookies.ContainsKey("pd240_session"))
        {
            return RedirectToAction("Index", "Home");
        }
        
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    public IActionResult Register()
    {
        return View();
    }

    public IActionResult ForgotPassword()
    {
        return View();
    }

    public IActionResult Logout()
    {
        // Remove cookie de sessão se existir
        Response.Cookies.Delete("pd240_session");
        return View();
    }

    public IActionResult AccessDenied()
    {
        return View();
    }

    /// <summary>
    /// Página de gerenciamento de conta do usuário
    /// </summary>
    public IActionResult Profile()
    {
        return View();
    }

    /// <summary>
    /// Página de configurações (chaves de API, preferências)
    /// </summary>
    public IActionResult Settings()
    {
        return View();
    }
}
