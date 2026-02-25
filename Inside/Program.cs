using PixelDisplay240Api.Models;
using PixelDisplay240Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
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

// Register services
builder.Services.AddSingleton<AgentConfigService>();
builder.Services.AddSingleton<LogService>();
builder.Services.AddSingleton<PrototypeService>();
builder.Services.AddSingleton<HardwareExportService>();
builder.Services.AddHttpClient<AIService>();
builder.Services.AddAuthorization();
builder.Services.AddRazorPages();
builder.Services.AddControllersWithViews();

builder.Services.AddWebOptimizer(pipeline =>
{
    pipeline.AddJavaScriptBundle("/js/bundle.js", 
        "js/model.js", 
        "js/tft-commands.js", 
        "js/view.js", 
        "js/controller.js", 
        "js/designer.js", 
        "js/script.js");
});

var authSection = builder.Configuration.GetSection("Auth");
var jwtKey = authSection["JwtKey"] ?? "CHANGE_ME_LONG_RANDOM_KEY_AT_LEAST_32_CHARS";
var issuer = authSection["Issuer"] ?? "PixelDisplay240";
var audience = authSection["Audience"] ?? "PixelDisplay240Clients";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
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

app.UseWebOptimizer();
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

api.MapGet("/agents", (AgentConfigService configService) =>
{
    var data = configService.LoadConfig();
    return Results.Json(data, jsonOptions);
});

api.MapPost("/agents", async (HttpRequest request, AgentConfigService configService) =>
{
    var data = await request.ReadFromJsonAsync<AgentConfig>(jsonOptions);
    if (data == null) return Results.BadRequest(new { message = "Invalid payload" });
    configService.SaveConfig(data);
    return Results.Json(new { ok = true }, jsonOptions);
});

api.MapPost("/logs", async (HttpRequest request, LogService logService) =>
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

api.MapPost("/config", async (HttpRequest request, AgentConfigService configService) =>
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

api.MapGet("/ai/image", async (string prompt, int? seed, AgentConfigService configService, AIService aiService) =>
{
    if (string.IsNullOrWhiteSpace(prompt)) return Results.BadRequest(new { message = "Prompt required" });

    var config = configService.LoadConfig();
    var apiKey = config.Gemini.ApiKey?.Trim() ?? string.Empty;
    var finalSeed = seed ?? new Random().Next(1, 1000000);

    var (success, bytes, error) = await aiService.GeneratePixelArtAsync(apiKey, prompt, finalSeed);

    if (!success)
    {
        var isQuota = (error ?? string.Empty).Contains("RESOURCE_EXHAUSTED", StringComparison.OrdinalIgnoreCase)
            || (error ?? string.Empty).Contains("quota", StringComparison.OrdinalIgnoreCase);

        if (enablePlaceholder && File.Exists(placeholderPath))
        {
            var fallbackBytes = await File.ReadAllBytesAsync(placeholderPath);
            return Results.File(fallbackBytes, "image/svg+xml");
        }

        var status = isQuota ? StatusCodes.Status429TooManyRequests : StatusCodes.Status502BadGateway;
        var message = isQuota ? "Quota exceeded" : "AI error";
        return Results.Json(new { message, body = error }, statusCode: status);
    }

    return Results.File(bytes!, "image/png");
});

api.MapPost("/ai/auto-layout", async (HttpRequest request, AgentConfigService configService, AIService aiService) =>
{
    using var doc = await JsonDocument.ParseAsync(request.Body);
    var root = doc.RootElement;
    var elementsJson = root.GetProperty("elements").GetRawText();
    var intent = root.TryGetProperty("intent", out var p) ? p.GetString() ?? "" : "";

    var config = configService.LoadConfig();
    var apiKey = config.Gemini.ApiKey?.Trim() ?? string.Empty;

    var (success, resultJson, error) = await aiService.OptimizeLayoutAsync(apiKey, elementsJson, intent);

    if (!success) return Results.Problem(error ?? "AI Optimization failed", statusCode: 502);
    
    return Results.Content(resultJson!, "application/json");
});

// --- PROTOTYPE API (Unified Pattern) ---
api.MapGet("/prototype", (PrototypeService service) => Results.Json(service.GetProject(), jsonOptions));

api.MapPost("/prototype/save", async (HttpRequest request, PrototypeService service) => {
    try {
        var project = await request.ReadFromJsonAsync<PrototypeProject>(jsonOptions);
        if (project != null) service.SaveProject(project);
        return Results.Ok(service.GetProject());
    } catch (Exception e) {
        return Results.Problem(e.Message);
    }
});

api.MapGet("/prototype/export", (HardwareExportService exportService, PrototypeService service) => {
    var project = service.GetProject();
    var zipBytes = exportService.GenerateProjectZip(project);
    return Results.File(zipBytes, "application/zip", "PixelDisplay240_Project.zip");
});

app.Run();

record AuthRequest(string ApiKey);
