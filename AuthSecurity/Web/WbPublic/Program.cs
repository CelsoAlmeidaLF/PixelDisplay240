using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Systekna.Kernel.Application.Services;
using Systekna.Kernel.Application.UseCases;
using Systekna.Kernel.Domain.DTOs;
using Systekna.Kernel.Domain.Entities;
using Systekna.Kernel.Domain.Interfaces;
using Systekna.Kernel.Infrastructure.Data;
using Systekna.Kernel.Infrastructure.Repositories;
using Systekna.Kernel.Infrastructure.Security_;
using System.Text;
using WbPublic.Endpoints;

var builder = WebApplication.CreateBuilder(args);

// Register Infrastructure
#if DEBUG
builder.Services.AddDbContext<AuthDbContext>(opt => 
    opt.UseInMemoryDatabase("AuthDb"));
#else
var connectionString = builder.Configuration.GetConnectionString("AuthConnection");
var serverVersion = ServerVersion.AutoDetect(connectionString);

builder.Services.AddDbContext<AuthDbContext>(opt => 
    opt.UseMySql(connectionString, serverVersion));
#endif

#region 'register services'

// Register Repositories
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IAuditRepository, AuditRepository>();
builder.Services.AddScoped<ISettingRepository, SettingRepository>();

// Register Security Services
builder.Services.AddScoped<IPasswordHasher, MyPasswordHasher>();
builder.Services.AddScoped<ITokenProvider, MyJwtTokenProvider>();

// Register Application Services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IVpsManagerService, VpsManagerService>();

// Register Use Cases
builder.Services.AddScoped<LoginUseCase>();

#if DEBUG
builder.Services.AddScoped<IEmailService, MockEmailService>();
#else
builder.Services.AddScoped<IEmailService, SmtpEmailService>();
#endif

#endregion

// Configure JWT Authentication
var jwtSettings = builder.Configuration.GetSection("Jwt");
var keyBytes = Encoding.UTF8.GetBytes(jwtSettings["Key"] ?? "dev-key-change-this-to-secure-random-value-32chars");

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
            IssuerSigningKey = new SymmetricSecurityKey(keyBytes)
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// Seed data for DEBUG
#if DEBUG
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
    var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
    if (!context.Users.Any())
    {
        context.Users.Add(new UserEntity
        {
            Username = "admin",
            Email = "admin@systekna.com",
            PasswordHash = hasher.Hash("admin123"),
            Role = "Admin",
            IsEmailVerified = true,
            IsApproved = true
        });
        context.SaveChanges();
        Console.WriteLine(">>> WbPublic DEBUG: Admin user created: admin / admin123");
    }
}
#endif

app.UseMiddleware<GlobalExceptionMiddleware>();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();

// Minimal API Endpoints organized in separate files
app.MapAuthEndpoints();
app.MapPublicEndpoints();
app.MapNavigationEndpoints();

app.Run();
