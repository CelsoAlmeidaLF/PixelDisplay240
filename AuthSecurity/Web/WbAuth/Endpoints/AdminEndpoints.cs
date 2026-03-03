using Systekna.Kernel.Domain.DTOs;
using Systekna.Kernel.Domain.Interfaces;

namespace WbAuth.Endpoints;

public static class AdminEndpoints
{
    public static void MapAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var adminGroup = app.MapGroup("/api/admin").RequireAuthorization(p => p.RequireRole("Admin"));

        adminGroup.MapGet("/users", async (IAuthService auth) => Results.Ok(await auth.GetAllUsers()));

        adminGroup.MapPut("/users/{id}/role", async (int id, UpdateRoleRequest req, IAuthService auth) =>
        {
            var success = await auth.UpdateUserRole(id, req.Role);
            return success ? Results.Ok(new { message = "User role updated" }) : Results.NotFound();
        });

        adminGroup.MapDelete("/users/{id}", async (int id, IAuthService auth) =>
        {
            var success = await auth.DeleteUser(id);
            return success ? Results.Ok(new { message = "User deleted" }) : Results.NotFound();
        });
    }
}
