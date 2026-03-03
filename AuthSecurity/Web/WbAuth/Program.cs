using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Systekna.Kernel.Application.UseCases;
using Systekna.Kernel.Extensions;
using Systekna.Kernel.Infrastructure.Security;
using System.Text;
using WbAuth.Endpoints;

var builder = WebApplication.CreateBuilder(args);

// === DATABASE E SERVICOS CENTRALIZADOS (Systekna.Kernel) ===
var connectionString = builder.Configuration.GetConnectionString("AuthConnection");
var isDevelopment = builder.Environment.IsDevelopment();

#if DEBUG
// DEBUG: Usar SQLite compartilhado (CentralAuth.db)
var sqlitePath = builder.Configuration.GetConnectionString("SqlitePath");
builder.Services.AddSysteknaSecuritySQLite(builder.Configuration, sqlitePath);
Console.WriteLine(">>> [WbAuth] DEBUG: Usando banco de dados SQLite compartilhado (CentralAuth.db)");
#else
if (!string.IsNullOrEmpty(connectionString) && connectionString != "InMemory")
{
    // PRODUCAO: Usar MySQL centralizado
    builder.Services.AddSysteknaSecurityMySQL(builder.Configuration, connectionString);
    Console.WriteLine(">>> [WbAuth] Usando banco de dados MySQL centralizado");
}
else
{
    // FALLBACK: InMemory (dados NAO persistem e NAO sao compartilhados entre processos)
    builder.Services.AddSysteknaSecurityInMemory(builder.Configuration, "CentralAuthDb");
    Console.WriteLine(">>> [WbAuth] ATENCAO: Usando banco em MEMORIA (nao compartilhado)");
}
#endif

// Register Use Cases (especificos deste projeto)
builder.Services.AddScoped<LoginUseCase>();
builder.Services.AddScoped<RegisterUserUseCase>();

// Configure JWT Authentication
var jwtSettings = builder.Configuration.GetSection("Jwt");
var key = Encoding.UTF8.GetBytes(jwtSettings["Key"] ?? "dev-key-change-this-to-secure-random-value-32chars");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtSettings["Issuer"] ?? "wbapi",
            ValidateAudience = true,
            ValidAudience = jwtSettings["Audience"] ?? "wbapi_users",
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key)
        };
    });

builder.Services.AddAuthorization(options =>
{
    // WbAuth: Clientes (User) e Administradores podem acessar
    options.AddPolicy("ClienteOuAdmin", policy => policy.RequireRole("User", "Admin"));
    options.AddPolicy("PixelDisplayAccess", policy => policy.RequireRole("User", "Admin"));
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// === SEED DE USUARIOS PADRAO (DEBUG) ===
if (isDevelopment)
{
    await app.Services.SeedDefaultUsersAsync(new[] { "WbAuth", "BaseAccess" });
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<GlobalExceptionMiddleware>();

app.UseStaticFiles(); // Re-enabled for testing purposes

app.UseAuthentication();
app.UseAuthorization();

// Minimal API Endpoints organized in separate files
app.MapAuthEndpoints();
app.MapAdminEndpoints();
app.MapPublicEndpoints();
app.MapPixelDisplayEndpoints();

app.Run();

