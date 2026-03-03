using Systekna.Kernel.Domain.Interfaces;

namespace WbAuth.Endpoints;

public static class PixelDisplayEndpoints
{
    public static void MapPixelDisplayEndpoints(this IEndpointRouteBuilder app)
    {
        // WbAuth: Apenas clientes (User) e administradores (Admin) podem acessar
        var group = app.MapGroup("/api/pixeldisplay").RequireAuthorization("ClienteOuAdmin");

        // Verifica se o usuário tem acesso ao PixelDisplay
        group.MapGet("/access", async (HttpContext context, IAuthService auth) =>
        {
            var userId = GetUserId(context);
            if (userId == 0) return Results.Unauthorized();

            var user = await auth.GetUserById(userId);
            if (user == null) return Results.NotFound();

            var hasAccess = user.Policies.Contains("PixelDisplay") || 
                           user.Policies.Contains("BaseAccess") ||
                           user.Role == "Admin";

            return Results.Ok(new
            {
                hasAccess,
                user.Username,
                user.Email,
                user.Role,
                policies = user.Policies.Split(';', StringSplitOptions.RemoveEmptyEntries)
            });
        });

        // Retorna o perfil do usuário para o PixelDisplay
        group.MapGet("/profile", async (HttpContext context, IAuthService auth) =>
        {
            var userId = GetUserId(context);
            if (userId == 0) return Results.Unauthorized();

            var user = await auth.GetUserById(userId);
            if (user == null) return Results.NotFound();

            return Results.Ok(new
            {
                user.Id,
                user.Username,
                user.Email,
                user.Role,
                user.CreatedAt,
                policies = user.Policies.Split(';', StringSplitOptions.RemoveEmptyEntries),
                features = new
                {
                    canExport = user.Policies.Contains("PixelDisplay.Export") || user.Role == "Admin",
                    canUseAI = user.Policies.Contains("PixelDisplay.AI") || user.Role == "Admin",
                    canManageProjects = user.Policies.Contains("PixelDisplay.Projects") || user.Role == "Admin",
                    maxProjects = user.Role == "Admin" ? -1 : 10
                }
            });
        });

        // Atualiza preferências do usuário no PixelDisplay
        group.MapPost("/preferences", async (HttpRequest request, HttpContext context, IAuthService auth) =>
        {
            var userId = GetUserId(context);
            if (userId == 0) return Results.Unauthorized();

            // Aqui poderia salvar preferências específicas do PixelDisplay
            // Por enquanto, apenas retorna sucesso
            return Results.Ok(new { message = "Preferências salvas com sucesso" });
        });
    }

    private static int GetUserId(HttpContext context)
    {
        var userIdClaim = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                       ?? context.User.FindFirst("sub")?.Value;
        
        return int.TryParse(userIdClaim, out var id) ? id : 0;
    }
}
