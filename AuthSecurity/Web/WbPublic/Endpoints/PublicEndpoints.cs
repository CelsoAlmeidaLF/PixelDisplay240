using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System.Security.Claims;

namespace WbPublic.Endpoints;

public static class PublicEndpoints
{
    public static void MapPublicEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/hello", () => Results.Ok(new { message = "Olá do Minimal API!" }));

        app.MapGet("/api/time", () => Results.Ok(new { time = DateTime.UtcNow }));

        // Protected admin API endpoint
        app.MapGet("/api/admin/data", (ClaimsPrincipal user) =>
        {
            return Results.Ok(new { 
                secret = "informação administrativa", 
                serverTime = DateTime.UtcNow,
                user = user.Identity?.Name
            });
        }).RequireAuthorization();
    }
}
