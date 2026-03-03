# ?? Documentação Técnica - PixelDisplay240-Inside

**Versão:** 2.0  
**Framework:** .NET 8  
**Última Atualização:** Junho 2025

---

## ?? Índice

1. [Arquitetura Técnica](#arquitetura-técnica)
2. [Padrões de Projeto](#padrões-de-projeto)
3. [Fluxo de Requisições](#fluxo-de-requisições)
4. [Segurança em Profundidade](#segurança-em-profundidade)
5. [Entity Framework e Banco de Dados](#entity-framework-e-banco-de-dados)
6. [Injeção de Dependências](#injeção-de-dependências)
7. [Autenticação JWT Detalhada](#autenticação-jwt-detalhada)
8. [Sistema Multi-Tenant](#sistema-multi-tenant)
9. [Integração com Google Gemini](#integração-com-google-gemini)
10. [Exportação para Hardware](#exportação-para-hardware)
11. [Logging e Monitoramento](#logging-e-monitoramento)
12. [Deploy e Configuração](#deploy-e-configuração)
13. [Troubleshooting](#troubleshooting)
14. [API Reference](#api-reference)

---

## ??? Arquitetura Técnica

### Visão em Camadas

```
???????????????????????????????????????????????????????????????????
?                      CAMADA DE APRESENTAÇÃO                      ?
?  ???????????????????????    ??????????????????????????????????? ?
?  ?   PixelDisplay240    ?    ?         WbGovAdmin              ? ?
?  ?   Razor Pages + API  ?    ?        Minimal API              ? ?
?  ?   • Controllers      ?    ?        • Endpoints              ? ?
?  ?   • Views            ?    ?        • Swagger                ? ?
?  ?   • Endpoints        ?    ?                                 ? ?
?  ?   • wwwroot (JS/CSS) ?    ?                                 ? ?
?  ???????????????????????    ??????????????????????????????????? ?
???????????????????????????????????????????????????????????????????
                              ?
                              ?
???????????????????????????????????????????????????????????????????
?                      CAMADA DE APLICAÇÃO                         ?
?  ?????????????????????????????????????????????????????????????  ?
?  ?              Systekna.Application                          ?  ?
?  ?  • IPrototypeService    ? Gerenciamento de projetos       ?  ?
?  ?  • IAIService           ? Integração com Gemini           ?  ?
?  ?  • IHardwareExportService ? Exportação Arduino/ESP32      ?  ?
?  ?  • IAgentConfigService  ? Configuração de agentes IA      ?  ?
?  ?  • ILogService          ? Logging de erros                ?  ?
?  ?????????????????????????????????????????????????????????????  ?
???????????????????????????????????????????????????????????????????
                              ?
                              ?
???????????????????????????????????????????????????????????????????
?                         KERNEL (CORE)                            ?
?  ?????????????????????????????????????????????????????????????  ?
?  ?              Systekna.Security                             ?  ?
?  ?                                                            ?  ?
?  ?  DOMAIN                                                    ?  ?
?  ?  ??? Entities: UserEntity, RegisteredSystem, AuditLog     ?  ?
?  ?  ??? DTOs: AuthDTOs, SystemDTOs                           ?  ?
?  ?  ??? Interfaces: IAuthService, IGovernanceService, etc.   ?  ?
?  ?                                                            ?  ?
?  ?  APPLICATION                                               ?  ?
?  ?  ??? Services: AuthService, AuditService, etc.            ?  ?
?  ?  ??? UseCases: LoginUseCase, RegisterUserUseCase          ?  ?
?  ?                                                            ?  ?
?  ?  INFRASTRUCTURE                                            ?  ?
?  ?  ??? Data: AuthDbContext                                   ?  ?
?  ?  ??? Repositories: EfRepositories                          ?  ?
?  ?  ??? Security: Middlewares, Providers                      ?  ?
?  ?????????????????????????????????????????????????????????????  ?
???????????????????????????????????????????????????????????????????
                              ?
                              ?
???????????????????????????????????????????????????????????????????
?                    CAMADA DE PERSISTÊNCIA                        ?
?  ???????????????  ???????????????  ???????????????????????????  ?
?  ?   SQLite    ?  ?    MySQL    ?  ?       InMemory          ?  ?
?  ?   (Debug)   ?  ?  (Produção) ?  ?      (Fallback)         ?  ?
?  ???????????????  ???????????????  ???????????????????????????  ?
???????????????????????????????????????????????????????????????????
```

### Dependências entre Projetos

```
PixelDisplay240
    ??? Systekna.Application
    ?   ??? (Nenhuma dependência de projeto)
    ??? Systekna.Security
        ??? (Nenhuma dependência de projeto)

WbGovAdmin
    ??? Systekna.Security
        ??? (Nenhuma dependência de projeto)
```

---

## ?? Padrões de Projeto

### 1. Clean Architecture

A solução segue os princípios da Clean Architecture:

- **Domain Layer**: Entidades, interfaces e DTOs (sem dependências externas)
- **Application Layer**: Casos de uso e serviços de domínio
- **Infrastructure Layer**: Implementações de repositórios, DbContext, providers
- **Presentation Layer**: Controllers, Endpoints, Views

### 2. Repository Pattern

```csharp
// Interface (Domain)
public interface IUserRepository
{
    Task<UserEntity?> GetByIdAsync(int id);
    Task<UserEntity?> GetByUsernameAsync(string username);
    Task<UserEntity?> GetByEmailAsync(string email);
    Task<List<UserEntity>> GetAllAsync();
    Task AddAsync(UserEntity user);
    Task UpdateAsync(UserEntity user);
    Task DeleteAsync(int id);
}

// Implementação (Infrastructure)
public class UserRepository : IUserRepository
{
    private readonly AuthDbContext _context;
    
    public async Task<UserEntity?> GetByIdAsync(int id)
        => await _context.Users.FindAsync(id);
}
```

### 3. Options Pattern

```csharp
// Definição da classe de opções
public class AIOptions
{
    public const string SectionName = "AI";
    
    public string PlaceholderPath { get; set; } = "wwwroot/ai-placeholder.svg";
    public int MaxPromptLength { get; set; } = 500;
    public int TimeoutSeconds { get; set; } = 60;
    public string GeminiModel { get; set; } = "gemini-2.5-flash-image";
}

// Registro no DI
builder.Services.Configure<AIOptions>(builder.Configuration.GetSection(AIOptions.SectionName));

// Uso via IOptions<T>
public class AIService
{
    private readonly AIOptions _options;
    
    public AIService(IOptions<AIOptions> options)
    {
        _options = options.Value;
    }
}
```

### 4. Aggregate Pattern (DDD)

```csharp
// MasterPrototype como Aggregate Root
public class MasterPrototype
{
    public PrototypeProject Project { get; }
    
    public MasterPrototype(PrototypeProject project)
    {
        Project = project;
    }
    
    // Todas as operações passam pelo aggregate
    public PrototypeScreen AddScreen(string? name = null)
    {
        var screen = new PrototypeScreen
        {
            Id = $"screen_{Project.ScreenSeq++}",
            Name = name ?? $"Screen {Project.Screens.Count + 1}"
        };
        Project.Screens.Add(screen);
        Project.ActiveScreenId = screen.Id;
        return screen;
    }
    
    public PrototypeElement? AddElement(string screenId, string type, string? asset)
    {
        var screen = GetScreen(screenId);
        if (screen == null) return null;
        
        var element = new PrototypeElement
        {
            Id = $"el_{Project.ElementSeq++}",
            Type = type,
            Name = $"{type}_{Project.ElementSeq}",
            Asset = asset
        };
        screen.Elements.Add(element);
        Project.SelectedElementId = element.Id;
        return element;
    }
}
```

### 5. Extension Methods Pattern

```csharp
// Extensão para configuração centralizada
public static class SysteknaSecurityExtensions
{
    public static IServiceCollection AddSysteknaSecuritySQLite(
        this IServiceCollection services, 
        IConfiguration configuration, 
        string? sqlitePath = null)
    {
        var dbPath = sqlitePath ?? DefaultSqlitePath;
        
        // Garantir diretório existe
        var directory = Path.GetDirectoryName(dbPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        services.AddDbContext<AuthDbContext>(options =>
            options.UseSqlite($"Data Source={dbPath}"));

        RegisterSecurityServices(services, configuration, useMockEmail: true);
        
        return services;
    }
}
```

---

## ?? Fluxo de Requisições

### Pipeline de Middleware

```
Request
    ?
    ?
???????????????????????????????????????
?     SecurityHeadersMiddleware       ?  ? Headers OWASP
???????????????????????????????????????
    ?
    ?
???????????????????????????????????????
?    ThreatDetectionMiddleware        ?  ? Análise de ameaças
???????????????????????????????????????
    ?
    ? (opcional)
???????????????????????????????????????
?       ApiCsrfMiddleware             ?  ? Proteção CSRF
???????????????????????????????????????
    ?
    ?
???????????????????????????????????????
?    GlobalExceptionMiddleware        ?  ? Tratamento de erros
???????????????????????????????????????
    ?
    ?
???????????????????????????????????????
?         WebOptimizer                ?  ? Bundling JS/CSS
???????????????????????????????????????
    ?
    ?
???????????????????????????????????????
?        Static Files                 ?  ? Arquivos estáticos
???????????????????????????????????????
    ?
    ?
???????????????????????????????????????
?            CORS                     ?  ? Cross-Origin
???????????????????????????????????????
    ?
    ?
???????????????????????????????????????
?         Rate Limiter                ?  ? Limite de requisições
???????????????????????????????????????
    ?
    ?
???????????????????????????????????????
?        Authentication               ?  ? Validação JWT
???????????????????????????????????????
    ?
    ?
???????????????????????????????????????
?        Authorization                ?  ? Políticas de acesso
???????????????????????????????????????
    ?
    ?
???????????????????????????????????????
?         Endpoint / MVC              ?  ? Lógica de negócio
???????????????????????????????????????
    ?
    ?
Response
```

### Ordem de Registro no Program.cs

```csharp
var app = builder.Build();

// 1. Middlewares de segurança (PRIMEIRO)
app.UseSysteknaSecurityDefaults(app.Environment);

// 2. WebOptimizer (bundling)
app.UseWebOptimizer();

// 3. Arquivos estáticos
app.UseStaticFiles();

// 4. CORS
app.UseCors("AllowConfigured");

// 5. Rate Limiter
app.UseRateLimiter();

// 6. Autenticação
app.UseAuthentication();

// 7. Autorização
app.UseAuthorization();

// 8. Endpoints
app.MapRazorPages();
app.MapControllerRoute(...);
app.MapAllAuthEndpoints();
```

---

## ??? Segurança em Profundidade

### Camadas de Proteção

```
???????????????????????????????????????????????????????????????
?                    1. PERÍMETRO (HTTP)                       ?
?  • Security Headers (CSP, HSTS, X-Frame-Options)            ?
?  • CORS                                                      ?
?  • Rate Limiting                                             ?
???????????????????????????????????????????????????????????????
                              ?
                              ?
???????????????????????????????????????????????????????????????
?                  2. DETECÇÃO DE AMEAÇAS                      ?
?  • Análise de padrões (SQL Injection, XSS, Path Traversal)  ?
?  • User-Agent blocking                                       ?
?  • Bloqueio automático de IPs                               ?
???????????????????????????????????????????????????????????????
                              ?
                              ?
???????????????????????????????????????????????????????????????
?                    3. AUTENTICAÇÃO                           ?
?  • JWT com assinatura HMAC-SHA256                           ?
?  • Refresh Tokens                                            ?
?  • Rate Limiting de login                                    ?
???????????????????????????????????????????????????????????????
                              ?
                              ?
???????????????????????????????????????????????????????????????
?                    4. AUTORIZAÇÃO                            ?
?  • Role-based (User, Admin)                                 ?
?  • Policy-based (claims)                                     ?
?  • System-based (multi-tenant)                              ?
???????????????????????????????????????????????????????????????
                              ?
                              ?
???????????????????????????????????????????????????????????????
?                    5. AUDITORIA                              ?
?  • Log de ações críticas                                    ?
?  • IP tracking                                               ?
?  • User-Agent tracking                                       ?
???????????????????????????????????????????????????????????????
```

### Algoritmo de Detecção de Ameaças

```csharp
public ThreatLevel AnalyzeRequest(string ip, string path, string method, IHeaderDictionary headers)
{
    var score = 0;

    // 1. User-Agent suspeito
    var userAgent = headers.UserAgent.ToString().ToLowerInvariant();
    if (SuspiciousUserAgents.Any(ua => userAgent.Contains(ua)))
        score += 50;

    // 2. SQL Injection patterns
    var pathLower = path.ToLowerInvariant();
    if (SqlInjectionPatterns.Any(p => pathLower.Contains(p)))
        score += 80;

    // 3. XSS patterns
    if (XssPatterns.Any(p => pathLower.Contains(p)))
        score += 70;

    // 4. Path Traversal patterns
    if (PathTraversalPatterns.Any(p => pathLower.Contains(p)))
        score += 90;

    // 5. Taxa de requisições
    if (_requestPatterns.TryGetValue(ip, out var pattern))
    {
        if (pattern.RequestsInWindow > _options.MaxRequestsPerMinute)
            score += 30;
        if (pattern.RequestsInWindow > _options.MaxRequestsPerMinute * 3)
            score += 50;
    }

    // 6. Taxa de erros
    if (_failedRequests.TryGetValue(ip, out var failures) && failures > _options.MaxFailuresBeforeBlock)
        score += 40;

    // 7. Headers suspeitos
    if (string.IsNullOrEmpty(userAgent))
        score += 20;
    if (headers["X-Forwarded-For"].Count > 5) // Header spoofing
        score += 30;

    return score switch
    {
        >= 100 => ThreatLevel.Critical,  // Bloqueio automático
        >= 70 => ThreatLevel.High,
        >= 40 => ThreatLevel.Medium,
        >= 20 => ThreatLevel.Low,
        _ => ThreatLevel.None
    };
}
```

### Login Rate Limiter

```csharp
public class LoginRateLimiter : ILoginRateLimiter
{
    private readonly ConcurrentDictionary<string, LoginAttempt> _attempts = new();
    private readonly RateLimitPolicy _policy;

    public async Task<RateLimitResult> CheckAsync(string identifier)
    {
        var now = DateTime.UtcNow;
        
        if (_attempts.TryGetValue(identifier, out var attempt))
        {
            // Verificar se está bloqueado
            if (attempt.BlockedUntil > now)
            {
                var remainingTime = attempt.BlockedUntil - now;
                return RateLimitResult.Blocked(remainingTime);
            }

            // Reset se passou a janela de tempo
            if (now - attempt.FirstAttempt > _policy.Window)
            {
                _attempts[identifier] = new LoginAttempt { FirstAttempt = now, Count = 1 };
                return RateLimitResult.Allowed();
            }

            // Verificar limite
            if (attempt.Count >= _policy.MaxAttempts)
            {
                // Bloqueio progressivo
                var blockDuration = TimeSpan.FromMinutes(_policy.BlockMinutes * Math.Pow(2, attempt.BlockCount));
                attempt.BlockedUntil = now.Add(blockDuration);
                attempt.BlockCount++;
                return RateLimitResult.Blocked(blockDuration);
            }

            attempt.Count++;
        }
        else
        {
            _attempts[identifier] = new LoginAttempt { FirstAttempt = now, Count = 1 };
        }

        return RateLimitResult.Allowed();
    }
}
```

---

## ?? Entity Framework e Banco de Dados

### DbContext

```csharp
public class AuthDbContext : DbContext
{
    public DbSet<UserEntity> Users { get; set; } = null!;
    public DbSet<AuditLog> AuditLogs { get; set; } = null!;
    public DbSet<ErrorLog> ErrorLogs { get; set; } = null!;
    public DbSet<SystemSetting> Settings { get; set; } = null!;
    public DbSet<RegisteredSystem> RegisteredSystems { get; set; } = null!;
    public DbSet<UserSystemAccess> UserSystemAccesses { get; set; } = null!;

    public AuthDbContext(DbContextOptions<AuthDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Índices únicos
        modelBuilder.Entity<UserEntity>()
            .HasIndex(u => u.Username)
            .IsUnique();
        
        modelBuilder.Entity<UserEntity>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.Entity<RegisteredSystem>()
            .HasIndex(s => s.SystemCode)
            .IsUnique();

        // Relacionamento N:N
        modelBuilder.Entity<UserSystemAccess>()
            .HasOne(usa => usa.User)
            .WithMany()
            .HasForeignKey(usa => usa.UserId);

        modelBuilder.Entity<UserSystemAccess>()
            .HasOne(usa => usa.System)
            .WithMany(s => s.UserAccesses)
            .HasForeignKey(usa => usa.SystemId);
    }
}
```

### Estratégia de Migração (SQLite em Debug)

```csharp
private static async Task EnsureDatabaseSchemaAsync(AuthDbContext context)
{
    var isSqlite = context.Database.ProviderName?.Contains("Sqlite", StringComparison.OrdinalIgnoreCase) == true;
    
    if (isSqlite)
    {
        var canConnect = await context.Database.CanConnectAsync();
        
        if (canConnect)
        {
            var connection = context.Database.GetDbConnection();
            var needsRecreation = false;
            
            await connection.OpenAsync();
            
            try
            {
                using var command = connection.CreateCommand();
                command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='RegisteredSystems';";
                var result = await command.ExecuteScalarAsync();
                needsRecreation = result == null;
            }
            finally
            {
                // IMPORTANTE: Fechar ANTES de deletar
                await connection.CloseAsync();
            }
            
            if (needsRecreation)
            {
                Console.WriteLine(">>> [DB] Schema desatualizado. Recriando...");
                await context.Database.EnsureDeletedAsync();
                await context.Database.EnsureCreatedAsync();
            }
        }
        else
        {
            await context.Database.EnsureCreatedAsync();
        }
    }
    else
    {
        await context.Database.EnsureCreatedAsync();
    }
}
```

---

## ?? Injeção de Dependências

### Lifetimes

| Lifetime | Uso | Exemplos |
|----------|-----|----------|
| **Singleton** | Estado compartilhado, stateless | `IPasswordHasher`, `ITokenProvider`, `ILoginRateLimiter` |
| **Scoped** | Uma instância por requisição | `AuthDbContext`, `IAuthService`, `IAuditService` |
| **Transient** | Nova instância a cada resolução | Factories, objetos leves |

### Registro de Serviços

```csharp
private static void RegisterSecurityServices(IServiceCollection services, IConfiguration configuration, bool useMockEmail)
{
    // Repositórios (Scoped - dependem do DbContext)
    services.AddScoped<IUserRepository, UserRepository>();
    services.AddScoped<IAuditRepository, AuditRepository>();
    services.AddScoped<ISettingRepository, SettingRepository>();
    services.AddScoped<IRegisteredSystemRepository, RegisteredSystemRepository>();
    services.AddScoped<IUserSystemAccessRepository, UserSystemAccessRepository>();

    // Providers de segurança (Singleton - stateless)
    services.AddSingleton<IPasswordHasher, MyPasswordHasher>();
    services.AddSingleton<ITokenProvider, MyJwtTokenProvider>();
    
    // Rate Limiter (Singleton - mantém estado em memória)
    services.AddSingleton<ILoginRateLimiter>(sp =>
    {
#if DEBUG
        return new LoginRateLimiter(RateLimitPolicy.Development);
#else
        return new LoginRateLimiter(RateLimitPolicy.Default);
#endif
    });
    
    // Validador de senhas (Singleton)
    services.AddSingleton<IPasswordValidationService>(sp =>
    {
#if DEBUG
        return new PasswordValidationService(PasswordPolicy.Development);
#else
        return new PasswordValidationService(PasswordPolicy.Default);
#endif
    });
    
    // E-mail (Scoped)
    if (useMockEmail)
        services.AddScoped<IEmailService, MockEmailService>();
    else
        services.AddScoped<IEmailService, SmtpEmailService>();
    
    // Serviços de aplicação (Scoped)
    services.AddScoped<IAuthService, AuthService>();
    services.AddScoped<IAuditService, AuditService>();
    services.AddScoped<IGovernanceService, GovernanceService>();
    services.AddScoped<IVpsManagerService, VpsManagerService>();
    services.AddScoped<ISystemRegistryService, SystemRegistryService>();
}
```

---

## ?? Autenticação JWT Detalhada

### Geração de Token

```csharp
public class MyJwtTokenProvider : ITokenProvider
{
    private readonly IConfiguration _configuration;

    public string CreateToken(string userId, string username, string email, string role, string? policies = null)
    {
        var key = _configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key not configured");
        var issuer = _configuration["Jwt:Issuer"] ?? "PixelDisplay240";
        var audience = _configuration["Jwt:Audience"] ?? "PixelDisplay240Clients";
        var expirationHours = int.Parse(_configuration["Auth:TokenExpirationHours"] ?? "4");

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId),
            new Claim(JwtRegisteredClaimNames.UniqueName, username),
            new Claim(JwtRegisteredClaimNames.Email, email),
            new Claim(ClaimTypes.Role, role),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };

        // Adicionar policies como claims múltiplas
        if (!string.IsNullOrEmpty(policies))
        {
            foreach (var policy in policies.Split(';', StringSplitOptions.RemoveEmptyEntries))
            {
                claims.Add(new Claim("policy", policy.Trim()));
            }
        }

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(expirationHours),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var randomNumber = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

    public ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
    {
        var key = _configuration["Jwt:Key"]!;
        
        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = false,
            ValidateIssuer = false,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            ValidateLifetime = false // Permite token expirado
        };

        var principal = new JwtSecurityTokenHandler().ValidateToken(token, tokenValidationParameters, out var securityToken);
        
        if (securityToken is not JwtSecurityToken jwtSecurityToken || 
            !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
        {
            throw new SecurityTokenException("Invalid token");
        }

        return principal;
    }
}
```

### Configuração de Validação

```csharp
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
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.Zero // Sem tolerância de tempo
        };
        
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
                {
                    context.Response.Headers.Add("Token-Expired", "true");
                }
                return Task.CompletedTask;
            }
        };
    });
```

---

## ?? Sistema Multi-Tenant

### Estrutura de Acesso

```
???????????????????????????????????????????????????????????????
?                         USUÁRIO                              ?
?  ????????????????????????????????????????????????????????????
?  ? Id: 1                                                    ??
?  ? Username: joao                                           ??
?  ? Role: User (Global)                                      ??
?  ? Policies: BaseAccess;WbAuth (Global)                    ??
?  ????????????????????????????????????????????????????????????
?                              ?                               ?
?           ???????????????????????????????????????           ?
?           ?                  ?                  ?           ?
?  ?????????????????? ?????????????????? ??????????????????  ?
?  ? pixeldisplay240? ?   wbgovadmin   ? ?     wbauth     ?  ?
?  ? SystemRole:User? ? (Sem Acesso)   ? ? SystemRole:User?  ?
?  ? Policies:      ? ?                ? ? Policies:      ?  ?
?  ?  Export;AI     ? ?                ? ?  BaseAccess    ?  ?
?  ? IsApproved: ?  ? ?                ? ? IsApproved: ?  ?  ?
?  ?????????????????? ?????????????????? ??????????????????  ?
???????????????????????????????????????????????????????????????
```

### Verificação de Acesso

```csharp
public async Task<bool> HasAccessAsync(int userId, string systemCode)
{
    var access = await _accessRepository.GetByUserAndSystemCodeAsync(userId, systemCode);
    return access != null && access.IsActive && access.IsApproved;
}

public async Task<string?> GetUserPoliciesForSystemAsync(int userId, string systemCode)
{
    var access = await _accessRepository.GetByUserAndSystemCodeAsync(userId, systemCode);
    
    if (access == null || !access.IsActive || !access.IsApproved)
        return null;

    // Combina políticas do sistema com políticas concedidas ao usuário
    var system = await _systemRepository.GetByCodeAsync(systemCode);
    var systemPolicies = system?.AvailablePolicies ?? "";
    var grantedPolicies = access.GrantedPolicies ?? "";

    // Retorna apenas as políticas que o usuário tem acesso
    var available = systemPolicies.Split(';', StringSplitOptions.RemoveEmptyEntries).ToHashSet();
    var granted = grantedPolicies.Split(';', StringSplitOptions.RemoveEmptyEntries);
    var intersection = granted.Where(p => available.Contains(p));
    
    return string.Join(";", intersection);
}
```

---

## ?? Integração com Google Gemini

### Fluxo de Geração de Imagem

```
????????????????
? Prompt User  ?
? "um gato"    ?
????????????????
       ?
       ?
????????????????????????????????????????????
?     STEP 1: Melhoria de Prompt           ?
?     Gemini 1.5 Flash (Text)              ?
?                                          ?
?  Input: "um gato"                        ?
?  System: "Transform this description     ?
?           into a professional pixel art  ?
?           prompt for a 240x240 display"  ?
?  Output: "cute pixel art cat, sitting,   ?
?           orange tabby, retro game style"?
????????????????????????????????????????????
       ?
       ?
????????????????????????????????????????????
?     STEP 2: Adicionar Instruções         ?
?                                          ?
?  Prompt Final:                           ?
?  "pixel art, 1:1 square, low resolution, ?
?   limited color palette, crisp edges,    ?
?   no gradients, cute pixel art cat..."   ?
????????????????????????????????????????????
       ?
       ?
????????????????????????????????????????????
?     STEP 3: Geração de Imagem            ?
?     Gemini 2.5 Flash Image               ?
?                                          ?
?  POST /v1beta/models/gemini-2.5-flash-   ?
?       image:generateContent              ?
?                                          ?
?  Headers:                                ?
?    x-goog-api-key: <API_KEY>             ?
????????????????????????????????????????????
       ?
       ?
????????????????????????????????????????????
?     STEP 4: Extração de Bytes            ?
?                                          ?
?  Response JSON:                          ?
?  {                                       ?
?    "candidates": [{                      ?
?      "content": {                        ?
?        "parts": [{                       ?
?          "inlineData": {                 ?
?            "mimeType": "image/png",      ?
?            "data": "<BASE64>"            ?
?          }                               ?
?        }]                                ?
?      }                                   ?
?    }]                                    ?
?  }                                       ?
?                                          ?
?  ? Convert.FromBase64String(data)        ?
????????????????????????????????????????????
       ?
       ?
????????????????????????????????????????????
?     STEP 5: Retorno                      ?
?                                          ?
?  return Results.File(bytes, "image/png") ?
????????????????????????????????????????????
```

### Tratamento de Erros

```csharp
api.MapGet("/ai/image", async (string prompt, int? seed, ...) =>
{
    // Validação
    if (string.IsNullOrWhiteSpace(prompt)) 
        return Results.BadRequest(new { message = "Prompt required" });
    
    if (prompt.Length > maxPromptLength)
        return Results.BadRequest(new { message = $"Max {maxPromptLength} characters" });

    var (success, bytes, error) = await aiService.GeneratePixelArtAsync(apiKey, prompt, seed);

    if (!success)
    {
        // Verificar se é quota excedida
        var isQuota = error?.Contains("RESOURCE_EXHAUSTED") == true ||
                      error?.Contains("quota") == true;

        // Fallback para placeholder SVG
        if (enablePlaceholder && File.Exists(placeholderPath))
        {
            var fallbackBytes = await File.ReadAllBytesAsync(placeholderPath);
            return Results.File(fallbackBytes, "image/svg+xml");
        }

        var status = isQuota ? 429 : 502;
        var message = isQuota ? "Quota exceeded" : "AI error";
        return Results.Json(new { message, body = error }, statusCode: status);
    }

    return Results.File(bytes!, "image/png");
})
.RequireRateLimiting("ai")
.RequireAuthorization("PixelDisplay.AI");
```

---

## ?? Exportação para Hardware

### Mapeamento de Cores

```csharp
// Conversão HTML (#RRGGBB) para RGB565 (TFT_eSPI)
private static string HtmlColorTo565(string htmlColor)
{
    if (string.IsNullOrEmpty(htmlColor) || !htmlColor.StartsWith("#")) 
        return "0x0000"; // Preto

    string hex = htmlColor.Substring(1);
    
    // Expandir #RGB para #RRGGBB
    if (hex.Length == 3) 
        hex = $"{hex[0]}{hex[0]}{hex[1]}{hex[1]}{hex[2]}{hex[2]}";

    int r = Convert.ToInt32(hex.Substring(0, 2), 16);
    int g = Convert.ToInt32(hex.Substring(2, 2), 16);
    int b = Convert.ToInt32(hex.Substring(4, 2), 16);

    // RGB888 ? RGB565
    int r5 = (r * 31) / 255;  // 5 bits para vermelho
    int g6 = (g * 63) / 255;  // 6 bits para verde
    int b5 = (b * 31) / 255;  // 5 bits para azul

    int rgb565 = (r5 << 11) | (g6 << 5) | b5;
    return "0x" + rgb565.ToString("X4");
}
```

### Geração de Código Arduino

```csharp
// Gera função para cada tela
foreach (var screen in project.Screens)
{
    sb.AppendLine($"void draw_{CleanName(screen.Name)}() {{");

    // Background
    if (!string.IsNullOrEmpty(screen.BackgroundAsset))
    {
        var asset = project.Assets.FirstOrDefault(a => a.Name == screen.BackgroundAsset);
        if (asset?.StorageType == "littlefs")
            sb.AppendLine($"    TJpg_Decoder.drawJpgFile(LittleFS, \"/{asset.Name}.jpg\", 0, 0);");
        else
            sb.AppendLine($"    tft.pushImage(0, 0, 240, 240, {asset.Name});");
    }
    else if (!string.IsNullOrEmpty(screen.BackgroundColor))
    {
        sb.AppendLine($"    tft.fillScreen({HtmlColorTo565(screen.BackgroundColor)});");
    }

    // Elementos
    foreach (var el in screen.Elements)
    {
        var color = HtmlColorTo565(el.Color);
        
        switch (el.Type.ToLower())
        {
            case "fillrect":
                sb.AppendLine($"    tft.fillRect({el.X}, {el.Y}, {el.W}, {el.H}, {color});");
                break;
            case "fillcircle":
                int r = Math.Min(el.W, el.H) / 2;
                sb.AppendLine($"    tft.fillCircle({el.X + el.W/2}, {el.Y + el.H/2}, {r}, {color});");
                break;
            case "drawstring":
                sb.AppendLine($"    tft.setTextColor({color});");
                sb.AppendLine($"    tft.drawString(\"{el.Name}\", {el.X}, {el.Y});");
                break;
            // ... outros tipos
        }
    }

    sb.AppendLine("}");
}
```

---

## ?? Logging e Monitoramento

### Estrutura de Logs de Auditoria

```csharp
public class AuditLog
{
    public int Id { get; set; }
    public string Action { get; set; }          // LOGIN_SUCCESS, USER_CREATED, etc.
    public string UserIdentifier { get; set; }  // Username ou e-mail
    public string Details { get; set; }         // Descrição detalhada
    public string IpAddress { get; set; }       // IP da requisição
    public string UserAgent { get; set; }       // Browser/cliente
    public DateTime Timestamp { get; set; }
}
```

### Eventos Auditados

| Ação | Descrição | Dados Registrados |
|------|-----------|-------------------|
| `LOGIN_SUCCESS` | Login bem-sucedido | Username, Role, IP |
| `LOGIN_FAILED` | Credenciais inválidas | Username, IP |
| `LOGIN_DENIED` | Role não permitido | Username, Role, IP |
| `REGISTER_SUCCESS` | Novo usuário | Username, Email, IP |
| `PASSWORD_CHANGED` | Senha alterada | Username, IP |
| `USER_APPROVED` | Usuário aprovado | Admin, User ID |
| `USER_DEACTIVATED` | Usuário bloqueado | Admin, User ID |
| `ADMIN_PASSWORD_RESET` | Reset pelo admin | Admin, User ID |

### Exportação de Relatórios

```csharp
public async Task<byte[]> GenerateAuditReportCsvAsync()
{
    var logs = await _auditRepository.GetAllAsync();
    var sb = new StringBuilder();
    
    // Header
    sb.AppendLine("Id,Action,User,Details,IP,UserAgent,Timestamp");
    
    // Dados
    foreach (var log in logs)
    {
        sb.AppendLine($"\"{log.Id}\",\"{log.Action}\",\"{log.UserIdentifier}\",\"{log.Details}\",\"{log.IpAddress}\",\"{log.UserAgent}\",\"{log.Timestamp:yyyy-MM-dd HH:mm:ss}\"");
    }
    
    return Encoding.UTF8.GetBytes(sb.ToString());
}
```

---

## ?? Deploy e Configuração

### Checklist de Produção

- [ ] Alterar `ASPNETCORE_ENVIRONMENT` para `Production`
- [ ] Configurar JWT Key forte (mínimo 256 bits)
- [ ] Configurar MySQL com SSL
- [ ] Configurar SMTP real
- [ ] Habilitar HTTPS obrigatório
- [ ] Configurar CORS para domínios específicos
- [ ] Desabilitar Swagger em produção
- [ ] Configurar backups do banco de dados
- [ ] Configurar monitoramento (Application Insights, etc.)
- [ ] Revisar rate limits para produção

### Variáveis de Ambiente Recomendadas

```bash
# Ambiente
ASPNETCORE_ENVIRONMENT=Production

# Banco de Dados
ConnectionStrings__AuthConnection="Server=db.example.com;Database=auth;User Id=app;Password=xxx;SslMode=Required"

# JWT (usar gerador de chaves criptográficas)
Jwt__Key="<256-bit-random-key>"
Jwt__Issuer="PixelDisplay240"
Jwt__Audience="PixelDisplay240Users"

# SMTP
Smtp__Host="smtp.sendgrid.net"
Smtp__Port="587"
Smtp__Username="apikey"
Smtp__Password="<sendgrid-api-key>"
Smtp__From="noreply@pixeldisplay240.com"

# IA (opcional)
AI__GeminiApiKey="<google-cloud-api-key>"
```

---

## ?? Troubleshooting

### Erro: IOException ao recriar banco SQLite

**Sintoma**: `The process cannot access the file 'CentralAuth.db' because it is being used by another process`

**Causa**: Conexão aberta ao tentar deletar o arquivo.

**Solução**: Fechar conexão antes de `EnsureDeletedAsync()`:
```csharp
try
{
    // ... verificações
}
finally
{
    await connection.CloseAsync(); // ANTES de sair do try
}

if (needsRecreation)
{
    await context.Database.EnsureDeletedAsync();
    await context.Database.EnsureCreatedAsync();
}
```

### Erro: Token inválido após deploy

**Sintoma**: Tokens válidos em dev falham em produção

**Causa**: JWT Key diferente entre ambientes

**Solução**: Usar mesma chave ou invalidar tokens antigos

### Erro: CORS bloqueando requisições

**Sintoma**: `Access-Control-Allow-Origin` não presente

**Causa**: Origem não configurada

**Solução**: Adicionar origem em `appsettings.json`:
```json
{
  "Cors": {
    "AllowedOrigins": ["https://seudominio.com"]
  }
}
```

### Erro: Rate limit muito restritivo

**Sintoma**: Usuários legítimos sendo bloqueados

**Causa**: Limites muito baixos para produção

**Solução**: Ajustar em `appsettings.json`:
```json
{
  "RateLimiting": {
    "PermitLimit": 200,
    "WindowSeconds": 60,
    "QueueLimit": 20
  }
}
```

---

## ?? API Reference

### DTOs de Autenticação

```csharp
// Requisições
public record LoginRequest(string Username, string Password);
public record RegisterRequest(string Username, string Email, string Password);
public record ForgotPasswordRequest(string Email);
public record ResetPasswordRequest(string Token, string NewPassword);
public record ChangePasswordRequest(string OldPassword, string NewPassword);
public record RefreshTokenRequest(string Token, string RefreshToken);

// Respostas
public record AuthResponse(
    string Token, 
    string RefreshToken, 
    string Username, 
    string Email, 
    string Role, 
    string RedirectUrl, 
    string Message
);

public record UserDto(
    int Id, 
    string Username, 
    string Email, 
    string Role, 
    bool IsEmailVerified, 
    DateTime CreatedAt, 
    bool IsApproved, 
    string Policies, 
    bool IsActive
);
```

### DTOs de Sistemas

```csharp
public record RegisteredSystemDto(
    int Id,
    string SystemCode,
    string DisplayName,
    string? Description,
    string? BaseUrl,
    string? IconUrl,
    string AvailablePolicies,
    bool IsActive,
    bool RequiresApproval,
    DateTime CreatedAt,
    int TotalUsers,
    int ActiveUsers
);

public record CreateSystemRequest(
    string SystemCode,
    string DisplayName,
    string? Description,
    string? BaseUrl,
    string? IconUrl,
    string? AvailablePolicies,
    bool RequiresApproval = true
);

public record GrantSystemAccessRequest(
    int UserId,
    int SystemId,
    string SystemRole,
    string? GrantedPolicies
);
```

### Códigos de Status

| Código | Significado | Quando Usar |
|--------|-------------|-------------|
| `200 OK` | Sucesso | GET, PUT com dados |
| `201 Created` | Recurso criado | POST com criação |
| `204 No Content` | Sucesso sem corpo | DELETE |
| `400 Bad Request` | Dados inválidos | Validação falhou |
| `401 Unauthorized` | Não autenticado | Token ausente/inválido |
| `403 Forbidden` | Sem permissão | Política não satisfeita |
| `404 Not Found` | Não encontrado | Recurso inexistente |
| `429 Too Many Requests` | Rate limit | Muitas requisições |
| `502 Bad Gateway` | Erro externo | Falha na API Gemini |

---

*Documentação Técnica v2.0 - Junho 2025*
