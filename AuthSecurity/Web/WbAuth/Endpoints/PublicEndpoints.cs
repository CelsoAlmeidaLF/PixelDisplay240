using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace WbAuth.Endpoints;

public static class PublicEndpoints
{
    public static void MapPublicEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/hello", () => Results.Ok(new { message = "Olá do Minimal API!" }));
        
        app.MapGet("/", () => Results.Redirect("/login.html"));
    }
}
