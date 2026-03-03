using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Systekna.Kernel.Application.UseCases;
using Systekna.Kernel.Extensions;
using Systekna.Kernel.Infrastructure.Security;
using System.Text;
using WbAdmin.Endpoints;
using WbGovAdmin.Endpoints;

var builder = WebApplication.CreateBuilder(args);

// === DATABASE E SERVICOS CENTRALIZADOS (Systekna.Kernel) ===
var connectionString = builder.Configuration.GetConnectionString("AuthConnection");
var isDevelopment = builder.Environment.IsDevelopment();

#if DEBUG
// DEBUG: Usar SQLite compartilhado (CentralAuth.db)
var sqlitePath = builder.Configuration.GetConnectionString("SqlitePath");
builder.Services.AddSysteknaSecuritySQLite(builder.Configuration, sqlitePath);
Console.WriteLine(">>> [WbGovAdmin] DEBUG: Usando banco de dados SQLite compartilhado (CentralAuth.db)");
#else
if (!string.IsNullOrEmpty(connectionString) && connectionString != "InMemory")
{
    // PRODUCAO: Usar MySQL centralizado
    builder.Services.AddSysteknaSecurityMySQL(builder.Configuration, connectionString);
    Console.WriteLine(">>> [WbGovAdmin] Usando banco de dados MySQL centralizado");
}
else
{
    // FALLBACK: InMemory (dados NAO persistem e NAO sao compartilhados entre processos)
    builder.Services.AddSysteknaSecurityInMemory(builder.Configuration, "CentralAuthDb");
    Console.WriteLine(">>> [WbGovAdmin] ATENCAO: Usando banco em MEMORIA (nao compartilhado)");
}
#endif

// === SERVICOS DE SEGURANCA ADICIONAIS ===
builder.Services.AddSysteknaSecurityServices(isDevelopment);

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
    // WbGovAdmin: Apenas administradores podem acessar este sistema
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    
    // Política GovAdmin: Admin OU usuário com policy específica
    options.AddPolicy("GovAdmin", policy => 
        policy.RequireAssertion(context =>
            context.User.IsInRole("Admin") || 
            context.User.HasClaim(c => c.Type == "policy" && 
                (c.Value == "WbGovAdmin" || c.Value == "GovAdmin"))));
    
    // Política padrão requer autenticação e role Admin
    options.FallbackPolicy = options.GetPolicy("AdminOnly");
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// === SEED DE USUARIOS PADRAO (DEBUG) ===
if (isDevelopment)
{
    await app.Services.SeedDefaultUsersAsync(new[] { "WbGovAdmin", "GovAdmin", "BaseAccess" });
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// === MIDDLEWARES DE SEGURANCA ===
app.UseSysteknaSecurityDefaults(app.Environment);

app.UseStaticFiles(); // Re-enabled for testing purposes

app.UseAuthentication();
app.UseAuthorization();

// Minimal API Endpoints organized in separate files
app.MapAuthEndpoints();
app.MapAdminEndpoints();
app.MapHostEndpoints();
app.MapReportEndpoints();
app.MapPublicEndpoints();
app.MapPixelDisplayAdminEndpoints();
app.MapSystemRegistryEndpoints();

app.Run();

