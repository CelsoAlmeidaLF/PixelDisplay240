using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace WbAdmin.Endpoints;

public static class PublicEndpoints
{
    public static void MapPublicEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/hello", () => Results.Ok(new { message = "Olá do Minimal API!" }))
            .AllowAnonymous();

        app.MapGet("/", () => Results.Redirect("/login.html"))
            .AllowAnonymous();
    }
}
