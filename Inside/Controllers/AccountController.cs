using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

namespace PixelDisplay240Api.Controllers;

public class AccountController : Controller
{
    public IActionResult Login(string? returnUrl = null)
    {
        // Se já estiver autenticado, redireciona para home
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Home");
        }
        
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    public IActionResult Register() => View();

    public IActionResult ForgotPassword() => View();

    public async Task<IActionResult> Logout()
    {
        // Remove o cookie de autenticação do ASP.NET Core
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        
        // Remove cookies adicionais se existirem
        Response.Cookies.Delete("PixelDisplay240.Auth");
        Response.Cookies.Delete("PixelDisplay240.Token");
        Response.Cookies.Delete("pd240_session");
        
        return View();
    }

    public IActionResult AccessDenied() => View();

    /// <summary>
    /// Página de gerenciamento de conta do usuário
    /// </summary>
    public IActionResult Profile() => View();

    /// <summary>
    /// Página de configurações (chaves de API, preferências)
    /// </summary>
    public IActionResult Settings() => View();
}
