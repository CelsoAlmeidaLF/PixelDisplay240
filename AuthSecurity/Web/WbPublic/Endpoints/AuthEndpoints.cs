using Systekna.Kernel.Application.UseCases;
using Systekna.Kernel.Domain.DTOs;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System.Security.Claims;

namespace WbPublic.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth");

        group.MapPost("/login", async (LoginRequest req, LoginUseCase useCase, HttpContext context) =>
        {
            if (string.IsNullOrWhiteSpace(req.Username) || string.IsNullOrWhiteSpace(req.Password))
                return Results.BadRequest(new { error = "Por favor, preencha todos os campos." });

            var ip = context.Connection.RemoteIpAddress?.ToString() ?? "";
            var userAgent = context.Request.Headers["User-Agent"].ToString();

            var response = await useCase.ExecuteAsync(req, ip, userAgent);
            return response != null ? Results.Ok(response) : Results.Unauthorized();
        });

        // Validate token - Simple check using ClaimsPrincipal
        group.MapGet("/validate", (ClaimsPrincipal user) => 
            Results.Ok(new { valid = user.Identity?.IsAuthenticated ?? false }));

        // Protected admin API endpoint - matches frontend call in admin.js
        group.MapGet("/protected", (ClaimsPrincipal user) =>
        {
            return Results.Ok(new { 
                secret = "Dados administrativos confidenciais", 
                serverTime = DateTime.UtcNow,
                status = "Authorized",
                user = user.Identity?.Name
            });
        }).RequireAuthorization();
    }
}
