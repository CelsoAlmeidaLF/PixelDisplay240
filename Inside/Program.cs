using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using PixelDisplay240Api.Endpoints;
using PixelDisplay240Api.Models;
using PixelDisplay240Api.Services;
using Systekna.Kernel.Extensions;
using System.Text;
using System.Text.Json;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    ContentRootPath = Directory.GetCurrentDirectory(),
    WebRootPath = "wwwroot"
});

// === DATABASE CONFIGURATION (Systekna.Security) ===
// Usar AuthConnection como padrão para centralizar dados de usuários
var connectionString = builder.Configuration.GetConnectionString("AuthConnection") 
    ?? builder.Configuration.GetConnectionString("DefaultConnection");
var isDevelopment = builder.Environment.IsDevelopment();

#if DEBUG
// DEBUG: Usar SQLite compartilhado (CentralAuth.db)
var sqlitePath = builder.Configuration.GetConnectionString("SqlitePath");
builder.Services.AddSysteknaSecuritySQLite(builder.Configuration, sqlitePath);
Console.WriteLine(">>> [PixelDisplay240] DEBUG: Usando banco de dados SQLite compartilhado (CentralAuth.db)");
#else
if (!string.IsNullOrEmpty(connectionString) && connectionString != "InMemory")
{
    // PRODUCAO: Usar MySQL centralizado
    builder.Services.AddSysteknaSecurityMySQL(builder.Configuration, connectionString);
    Console.WriteLine(">>> [PixelDisplay240] Usando banco de dados MySQL centralizado");
}
else
{
    // FALLBACK: InMemory (dados NAO persistem e NAO sao compartilhados entre processos)
    builder.Services.AddSysteknaSecurityInMemory(builder.Configuration, "CentralAuthDb");
    Console.WriteLine(">>> [PixelDisplay240] ATENCAO: Usando banco em MEMORIA (nao compartilhado)");
}
#endif

// === OPTIONS PATTERN - Configuracoes Tipadas ===
builder.Services.Configure<AuthOptions>(builder.Configuration.GetSection(AuthOptions.SectionName));
builder.Services.Configure<FeatureOptions>(builder.Configuration.GetSection(FeatureOptions.SectionName));
builder.Services.Configure<AIOptions>(builder.Configuration.GetSection(AIOptions.SectionName));
builder.Services.Configure<RateLimitingOptions>(builder.Configuration.GetSection(RateLimitingOptions.SectionName));
builder.Services.Configure<CorsOptions>(builder.Configuration.GetSection(CorsOptions.SectionName));

// === RATE LIMITING ===
var rateLimitConfig = builder.Configuration.GetSection(RateLimitingOptions.SectionName).Get<RateLimitingOptions>() ?? new();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    
    options.AddPolicy("api", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = rateLimitConfig.PermitLimit,
                Window = TimeSpan.FromSeconds(rateLimitConfig.WindowSeconds),
                QueueLimit = rateLimitConfig.QueueLimit,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst
            }));
    
    options.AddPolicy("ai", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 2,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst
            }));
});

// === CORS ===
var corsConfig = builder.Configuration.GetSection(CorsOptions.SectionName).Get<CorsOptions>() ?? new();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowConfigured", policy =>
    {
        if (corsConfig.AllowedOrigins.Contains("*"))
        {
            policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
        }
        else
        {
            policy.WithOrigins(corsConfig.AllowedOrigins)
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials();
        }
    });
});

// === HEALTH CHECKS ===
builder.Services.AddHealthChecks();

// === SERVICES (PixelDisplay240 Especificos) ===
builder.Services.AddSingleton<AgentConfigService>();
builder.Services.AddSingleton<LogService>();
builder.Services.AddSingleton<PrototypeService>();
builder.Services.AddSingleton<HardwareExportService>();

var aiConfig = builder.Configuration.GetSection(AIOptions.SectionName).Get<AIOptions>() ?? new();
builder.Services.AddHttpClient<AIService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(aiConfig.TimeoutSeconds);
});

builder.Services.AddAuthorization(options =>
{
    // PixelDisplay240: Clientes (User) e Administradores podem acessar
    options.AddPolicy("ClienteOuAdmin", policy => policy.RequireRole("User", "Admin"));
    options.AddPolicy("PixelDisplayAccess", policy => policy.RequireRole("User", "Admin"));
    
    // Políticas granulares baseadas em claims de policy do token JWT
    options.AddPolicy("PixelDisplay.AI", policy => 
        policy.RequireAssertion(context =>
            context.User.IsInRole("Admin") || 
            context.User.HasClaim(c => c.Type == "policy" && c.Value == "PixelDisplay.AI")));
    
    options.AddPolicy("PixelDisplay.Export", policy => 
        policy.RequireAssertion(context =>
            context.User.IsInRole("Admin") || 
            context.User.HasClaim(c => c.Type == "policy" && c.Value == "PixelDisplay.Export")));
    
    options.AddPolicy("PixelDisplay.Projects", policy => 
        policy.RequireAssertion(context =>
            context.User.IsInRole("Admin") || 
            context.User.HasClaim(c => c.Type == "policy" && c.Value == "PixelDisplay.Projects")));
});
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

// === AUTHENTICATION (JWT) ===
var jwtKey = builder.Configuration["Jwt:Key"] 
    ?? builder.Configuration["Auth:JwtKey"]
    ?? throw new InvalidOperationException("JWT Key must be configured");

var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "PixelDisplay240";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "PixelDisplay240Clients";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

var app = builder.Build();

// === SEED DE USUARIOS (DEBUG) ===
var seedDefaultUser = builder.Configuration.GetValue<bool>("Features:SeedDefaultUser");
if (isDevelopment || seedDefaultUser)
{
    await app.Services.SeedDefaultUsersAsync(new[]
    {
        "PixelDisplay",
        "PixelDisplay.Export",
        "PixelDisplay.AI",
        "PixelDisplay.Projects"
    });
}

var contentRoot = app.Environment.ContentRootPath;

// === MIDDLEWARE PIPELINE ===
app.UseWebOptimizer();
app.UseStaticFiles();

app.UseCors("AllowConfigured");
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

// === HEALTH CHECK ===
app.MapHealthChecks("/health");

var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);

// Carregar configuracoes para endpoints do PixelDisplay240
var featureOptions = app.Services.GetRequiredService<IOptions<FeatureOptions>>().Value;
var aiOptions = app.Services.GetRequiredService<IOptions<AIOptions>>().Value;

var enableLogs = featureOptions.EnableLogs;
var enablePlaceholder = featureOptions.EnablePlaceholder;
var placeholderRelPath = aiOptions.PlaceholderPath;
var placeholderPath = Path.Combine(contentRoot, placeholderRelPath.Replace('/', Path.DirectorySeparatorChar));
var maxPromptLength = aiOptions.MaxPromptLength;

app.MapRazorPages();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// ==========================================
// ENDPOINTS DE AUTENTICACAO E GOVERNANÇA
// Organizados em arquivos separados em /Endpoints
// ==========================================
app.MapAllAuthEndpoints();

// ==========================================
// API PROTEGIDA DO PIXELDISPLAY240
// ==========================================
// Apenas clientes (User) e administradores (Admin) podem acessar
var api = app.MapGroup("/api").RequireAuthorization("ClienteOuAdmin").RequireRateLimiting("api");

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
    
    if (string.IsNullOrEmpty(key)) return Results.BadRequest("Chave invalida");
    
    var config = configService.LoadConfig();
    if (key == "GeminiKey") config.Gemini.ApiKey = val ?? "";
    configService.SaveConfig(config);
    
    return Results.Ok(new { message = "Configuracao salva com seguranca!" });
});

api.MapGet("/ai/image", async (string prompt, int? seed, AgentConfigService configService, AIService aiService) =>
{
    if (string.IsNullOrWhiteSpace(prompt)) return Results.BadRequest(new { message = "Prompt required" });
    
    if (prompt.Length > maxPromptLength)
    {
        return Results.BadRequest(new { message = $"Prompt too long. Maximum {maxPromptLength} characters allowed." });
    }

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
}).RequireRateLimiting("ai").RequireAuthorization("PixelDisplay.AI");

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
}).RequireRateLimiting("ai").RequireAuthorization("PixelDisplay.AI");

// --- PROTOTYPE API ---
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
}).RequireAuthorization("PixelDisplay.Export");

app.Run();
