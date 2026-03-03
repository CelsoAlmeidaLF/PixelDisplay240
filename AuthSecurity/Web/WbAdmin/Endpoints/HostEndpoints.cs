using Systekna.Kernel.Domain.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System.Security.Claims;

namespace WbAdmin.Endpoints;

public static class HostEndpoints
{
    public static void MapHostEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/host").RequireAuthorization(p => p.RequireRole("Admin"));

        group.MapGet("/stats", async (IVpsManagerService vps) => 
            Results.Ok(await vps.GetHostStatsAsync()));

        group.MapPost("/restart", async (string serviceName, IVpsManagerService vps, IAuditService audit, ClaimsPrincipal user) =>
        {
            var success = await vps.RestartServiceAsync(serviceName);
            if (success)
            {
                await audit.LogAsync("VPS_RestartService", user.Identity?.Name ?? "Admin", $"Reiniciado serviço: {serviceName}");
                return Results.Ok(new { message = $"Serviço {serviceName} reiniciado com sucesso." });
            }
            return Results.BadRequest(new { error = "Falha ao reiniciar serviço." });
        });

        group.MapGet("/logs", async (IVpsManagerService vps) => 
            Results.Ok(await vps.GetSystemLogsAsync()));
            
        group.MapGet("/errors", async (IGovernanceService gov) => 
            Results.Ok(await gov.GetRecentErrorsAsync()));
    }
}
