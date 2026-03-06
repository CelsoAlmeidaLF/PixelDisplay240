using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Systekna.PixelDisplay.Application;
using Systekna.PixelDisplay.Application.Domain.Entities;
using Systekna.PixelDisplay.Application.Infrastructure.Interfaces;
using Systekna.PixelDisplay.Application.Services;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    ContentRootPath = Directory.GetCurrentDirectory(),
    WebRootPath = "wwwroot"
});

// Register services from Application layer
builder.Services.AddPixelDisplayApplication(builder.Environment.ContentRootPath);
builder.Services.AddAuthorization();
builder.Services.AddRazorPages();
builder.Services.AddControllersWithViews();

var authSection = builder.Configuration.GetSection("Auth");
var jwtKey = authSection["JwtKey"] ?? "CHANGE_ME_LONG_RANDOM_KEY_AT_LEAST_32_CHARS";
var issuer = authSection["Issuer"] ?? "PixelDisplay240";
var audience = authSection["Audience"] ?? "PixelDisplay240Clients";

object value1 = builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = issuer,
            ValidAudience = audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

var app = builder.Build();

var contentRoot = app.Environment.ContentRootPath;

app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);

var enableLogs = builder.Configuration.GetValue<bool>("Features:EnableLogs");
var enablePlaceholder = builder.Configuration.GetValue<bool>("Features:EnablePlaceholder");
var placeholderRelPath = builder.Configuration.GetValue<string>("AI:PlaceholderPath") ?? "wwwroot/ai-placeholder.svg";
var placeholderPath = Path.Combine(contentRoot, placeholderRelPath.Replace('/', Path.DirectorySeparatorChar));

app.MapRazorPages();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapGet("/api/auth/token", (HttpRequest request) =>
{
    var expires = DateTime.UtcNow.AddHours(4);
    var tokenHandler = new JwtSecurityTokenHandler();
    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
    var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

    var token = tokenHandler.CreateToken(new SecurityTokenDescriptor
    {
        Subject = new ClaimsIdentity(new[] { new Claim("sub", "pixeldisplay240") }),
        Expires = expires,
        Issuer = issuer,
        Audience = audience,
        SigningCredentials = creds
    });

    return Results.Json(new { token = tokenHandler.WriteToken(token), expiresAt = expires }, jsonOptions);
});

var api = app.MapGroup("/api").RequireAuthorization();

api.MapGet("/agents", (IAgentConfigRepository configService) =>
{
    var data = configService.LoadConfig();
    return Results.Json(data, jsonOptions);
});

api.MapPost("/agents", async (HttpRequest request, IAgentConfigRepository configService) =>
{
    var data = await request.ReadFromJsonAsync<AgentConfig>(jsonOptions);
    if (data == null) return Results.BadRequest(new { message = "Invalid payload" });
    configService.SaveConfig(data);
    return Results.Json(new { ok = true }, jsonOptions);
});

api.MapPost("/logs", async (HttpRequest request, ILogRepository logService) =>
{
    if (!enableLogs) return Results.NotFound();
    using var doc = await JsonDocument.ParseAsync(request.Body);
    var root = doc.RootElement;
    var context = root.GetProperty("Context").GetString() ?? "Unknown";
    var type = root.GetProperty("Type").GetString() ?? "ERROR";
    var message = root.GetProperty("Message").GetString() ?? "";
    var data = root.GetProperty("Data").GetRawText();
    logService.SaveError(context, type, message, data);
    return Results.Ok();
});

api.MapPost("/config", async (HttpRequest request, IAgentConfigRepository configService) =>
{
    using var doc = await JsonDocument.ParseAsync(request.Body);
    var root = doc.RootElement;
    var key = root.GetProperty("Key").GetString();
    var val = root.GetProperty("Value").GetString();
    
    if (string.IsNullOrEmpty(key)) return Results.BadRequest("Chave inválida");
    
    var config = configService.LoadConfig();
    if (key == "GeminiKey") config.Gemini.ApiKey = val ?? "";
    configService.SaveConfig(config);
    
    return Results.Ok(new { message = "Configuração salva com segurança!" });
});

api.MapGet("/ai/image", async (string prompt, int? seed, IAgentConfigRepository configService, IAIService aiService) =>
{
    if (string.IsNullOrWhiteSpace(prompt)) return Results.BadRequest(new { message = "Prompt required" });

    var config = configService.LoadConfig();
    var apiKey = config.Gemini.ApiKey?.Trim() ?? string.Empty;
    var finalSeed = seed ?? new Random().Next(1, 1000000);

    var result = await aiService.GeneratePixelArtAsync(apiKey, prompt, finalSeed);

    if (!result.Success)
    {
        var isQuota = (result.Error ?? string.Empty).Contains("RESOURCE_EXHAUSTED", StringComparison.OrdinalIgnoreCase)
            || (result.Error ?? string.Empty).Contains("quota", StringComparison.OrdinalIgnoreCase);

        if (enablePlaceholder && File.Exists(placeholderPath))
        {
            var fallbackBytes = await File.ReadAllBytesAsync(placeholderPath);
            return Results.File(fallbackBytes, "image/svg+xml");
        }

        var status = isQuota ? StatusCodes.Status429TooManyRequests : StatusCodes.Status502BadGateway;
        var message = isQuota ? "Quota exceeded" : "AI error";
        return Results.Json(new { message, body = result.Error }, statusCode: status);
    }

    return Results.File(result.Data!, "image/png");
});

api.MapPost("/ai/auto-layout", async (HttpRequest request, IAgentConfigRepository configService, IAIService aiService) =>
{
    using var doc = await JsonDocument.ParseAsync(request.Body);
    var root = doc.RootElement;
    var elementsJson = root.GetProperty("elements").GetRawText();
    var intent = root.TryGetProperty("intent", out var p) ? p.GetString() ?? "" : "";

    var config = configService.LoadConfig();
    var apiKey = config.Gemini.ApiKey?.Trim() ?? string.Empty;

    var result = await aiService.OptimizeLayoutAsync(apiKey, elementsJson, intent);

    if (!result.Success) return Results.Problem(result.Error ?? "AI Optimization failed", statusCode: 502);
    
    return Results.Content(result.ElementsJson!, "application/json");
});

api.MapGet("/prototype", (PrototypeApplicationService service) => Results.Json(service.GetProject(), jsonOptions));

api.MapPost("/prototype/save", async (HttpRequest request, PrototypeApplicationService service) => {
    try {
        var project = await request.ReadFromJsonAsync<PrototypeProject>(jsonOptions);
        if (project != null) service.SaveProject(project);
        return Results.Ok(service.GetProject());
    } catch (Exception e) {
        return Results.Problem(e.Message);
    }
});

api.MapPost("/prototype/screen/background", async (HttpRequest request, PrototypeApplicationService service) => {
    try {
        using var doc = await JsonDocument.ParseAsync(request.Body);
        var root = doc.RootElement;
        var screenId = root.GetProperty("screenId").GetString();
        var assetName = root.GetProperty("assetName").GetString();
        var dataUrl = root.GetProperty("dataUrl").GetString();

        if (string.IsNullOrEmpty(screenId)) return Results.BadRequest("ScreenId required");

        service.UpdateScreenBackground(screenId, assetName, dataUrl);
        return Results.Ok(new { success = true });
    } catch (Exception e) {
        return Results.Problem(e.Message);
    }
});

api.MapPost("/prototype/screen/update", async (HttpRequest request, PrototypeApplicationService service) => {
    try {
        using var doc = await JsonDocument.ParseAsync(request.Body);
        var root = doc.RootElement;
        var screenId = root.GetProperty("screenId").GetString();
        var updates = await request.ReadFromJsonAsync<Dictionary<string, object?>>(jsonOptions);

        if (string.IsNullOrEmpty(screenId) || updates == null) return Results.BadRequest("Invalid data");

        service.UpdateScreen(screenId, updates);
        return Results.Ok(new { success = true });
    } catch (Exception e) {
        return Results.Problem(e.Message);
    }
});

api.MapGet("/prototype/export", (HardwareExportApplicationService exportService, PrototypeApplicationService service) => {
    var project = service.GetProject();
    var zipBytes = exportService.GenerateProjectZip(project);
    return Results.File(zipBytes, "application/zip", "PixelDisplay240_Project.zip");
});

api.MapGet("/hardware/scan", () => {
    // Basic mock discovery for now as requested by UI
    return Results.Ok(new[] { 
        new { id = "COM3", name = "ESP32 PixelDisplay (COM3)", type = "Serial" },
        new { id = "192.168.1.100", name = "WiFi Display (192.168.1.100)", type = "Network" }
    });
});

api.MapPost("/hardware/export", async (HttpRequest request, HardwareExportApplicationService exportService) => {
    try {
        var project = await request.ReadFromJsonAsync<PrototypeProject>(jsonOptions);
        if (project == null) return Results.BadRequest("Invalid project data");
        var zipBytes = exportService.GenerateProjectZip(project);
        return Results.File(zipBytes, "application/zip", "PixelDisplay240_Hardware.zip");
    } catch (Exception e) {
        return Results.Problem(e.Message);
    }
});

var faviconSvg = "<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 16 16'><rect width='16' height='16' fill='%2338bdf8'/><text x='8' y='11' font-size='10' text-anchor='middle' fill='%23000' font-family='Arial'>PD</text></svg>";
app.MapGet("/favicon.svg", () => Results.Content(faviconSvg, "image/svg+xml"));
app.MapGet("/favicon.ico", () => Results.Content(faviconSvg, "image/svg+xml"));

app.Run();

record AuthRequest(string ApiKey);
