using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace WbPublic.Endpoints;

public static class NavigationEndpoints
{
    public static void MapNavigationEndpoints(this IEndpointRouteBuilder app)
    {
        // Serve login page at /login
        app.MapGet("/login", async (HttpContext ctx, IWebHostEnvironment env) =>
        {
            Console.WriteLine($"[/login] request from {ctx.Connection.RemoteIpAddress}");
            var file = System.IO.Path.Combine(env.WebRootPath ?? "wwwroot", "login.html");
            if (System.IO.File.Exists(file))
            {
                await ctx.Response.SendFileAsync(file);
            }
            else
            {
                Console.WriteLine("login.html not found at: " + file);
                ctx.Response.StatusCode = 404;
            }
        });

        // Redirect root to /login to make it easy to reach the login page
        app.MapGet("/", () => Results.Redirect("/login"));

        // Serve admin page at /admin
        app.MapGet("/admin", async (HttpContext ctx, IWebHostEnvironment env) =>
        {
            Console.WriteLine($"[/admin] request from {ctx.Connection.RemoteIpAddress}");
            var file = System.IO.Path.Combine(env.WebRootPath ?? "wwwroot", "admin.html");
            if (System.IO.File.Exists(file))
            {
                await ctx.Response.SendFileAsync(file);
            }
            else
            {
                Console.WriteLine("admin.html not found at: " + file);
                ctx.Response.StatusCode = 404;
            }
        });

        // Fallback to the single-page HTML
        app.MapFallbackToFile("index.html");
    }
}
