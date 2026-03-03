using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Systekna.Kernel.Application.Services;
using Systekna.Kernel.Domain.Entities;
using Systekna.Kernel.Domain.Interfaces;
using Systekna.Kernel.Infrastructure.Data;
using Systekna.Kernel.Infrastructure.Repositories;
using Systekna.Kernel.Infrastructure.Security;

namespace Systekna.Kernel.Extensions;

/// <summary>
/// Extension methods para configurar Systekna.Security em aplicacoes ASP.NET Core
/// </summary>
public static class SysteknaSecurityExtensions
{
    // Caminho padrao para o banco SQLite compartilhado
    private static readonly string DefaultSqlitePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Systekna",
        "CentralAuth.db");

    /// <summary>
    /// Adiciona os servicos de seguranca do Systekna com banco InMemory (DEBUG - NAO COMPARTILHADO)
    /// Usa MockEmailService para nao enviar emails reais
    /// ATENCAO: Dados NAO sao compartilhados entre processos diferentes!
    /// </summary>
    public static IServiceCollection AddSysteknaSecurityInMemory(this IServiceCollection services, IConfiguration configuration, string databaseName = "SysteknaAuthDb")
    {
        services.AddDbContext<AuthDbContext>(options =>
            options.UseInMemoryDatabase(databaseName));

        RegisterSecurityServices(services, configuration, useMockEmail: true);
        
        return services;
    }

    /// <summary>
    /// Adiciona os servicos de seguranca do Systekna com SQLite compartilhado (Desenvolvimento)
    /// Todos os projetos acessam o mesmo arquivo de banco de dados.
    /// Usa MockEmailService para nao enviar emails reais.
    /// </summary>
    public static IServiceCollection AddSysteknaSecuritySQLite(this IServiceCollection services, IConfiguration configuration, string? sqlitePath = null)
    {
        var dbPath = sqlitePath ?? configuration.GetConnectionString("SqlitePath") ?? DefaultSqlitePath;
        
        // Garantir que o diretorio existe
        var directory = Path.GetDirectoryName(dbPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var connectionString = $"Data Source={dbPath}";
        
        services.AddDbContext<AuthDbContext>(options =>
            options.UseSqlite(connectionString));

        RegisterSecurityServices(services, configuration, useMockEmail: true);
        
        Console.WriteLine($">>> [Systekna.Security] SQLite: {dbPath}");
        
        return services;
    }

    /// <summary>
    /// Adiciona os servicos de seguranca do Systekna com MySQL (Producao/Homologacao)
    /// Usa SmtpEmailService para enviar emails reais via SMTP
    /// </summary>
    public static IServiceCollection AddSysteknaSecurityMySQL(this IServiceCollection services, IConfiguration configuration, string connectionString)
    {
        var serverVersion = ServerVersion.AutoDetect(connectionString);
        services.AddDbContext<AuthDbContext>(options =>
            options.UseMySql(connectionString, serverVersion));

        RegisterSecurityServices(services, configuration, useMockEmail: false);
        
        return services;
    }

    private static void RegisterSecurityServices(IServiceCollection services, IConfiguration configuration, bool useMockEmail)
    {
        // Registrar repositorios
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IAuditRepository, AuditRepository>();
        services.AddScoped<ISettingRepository, SettingRepository>();
        services.AddScoped<IRegisteredSystemRepository, RegisteredSystemRepository>();
        services.AddScoped<IUserSystemAccessRepository, UserSystemAccessRepository>();

        // Registrar servicos de seguranca
        services.AddSingleton<IPasswordHasher, MyPasswordHasher>();
        services.AddSingleton<ITokenProvider, MyJwtTokenProvider>();
        
        // Rate Limiter para protecao contra forca bruta (Singleton para manter estado)
        services.AddSingleton<ILoginRateLimiter>(sp =>
        {
#if DEBUG
            return new LoginRateLimiter(RateLimitPolicy.Development);
#else
            return new LoginRateLimiter(RateLimitPolicy.Default);
#endif
        });
        
        // Validador de senhas
        services.AddSingleton<IPasswordValidationService>(sp =>
        {
#if DEBUG
            return new PasswordValidationService(PasswordPolicy.Development);
#else
            return new PasswordValidationService(PasswordPolicy.Default);
#endif
        });
        
        // Servico de email: Mock para DEBUG, SMTP real para Producao
        if (useMockEmail)
        {
            services.AddScoped<IEmailService, MockEmailService>();
        }
        else
        {
            services.AddScoped<IEmailService, SmtpEmailService>();
        }
        
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IGovernanceService, GovernanceService>();
        services.AddScoped<IVpsManagerService, VpsManagerService>();
        services.AddScoped<ISystemRegistryService, SystemRegistryService>();
    }

    /// <summary>
    /// Cria usuarios padrao para desenvolvimento (DEBUG)
    /// Aplica migrations automaticamente para SQLite
    /// Trata condicao de corrida quando multiplos projetos compartilham o mesmo banco
    /// </summary>
    public static async Task SeedDefaultUsersAsync(this IServiceProvider services, string[] policies)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        // Garantir que o banco esta criado (funciona para InMemory e SQLite)
        await context.Database.EnsureCreatedAsync();

        // Combina todas as policies em uma string separada por ponto e virgula
        var allPolicies = new HashSet<string>(policies)
        {
            "BaseAccess",
            "WbGovAdmin",
            "WbAuth", 
            "PixelDisplay",
            "PixelDisplay.AI",
            "PixelDisplay.Export",
            "PixelDisplay.Projects"
        };
        var adminPolicies = string.Join(";", allPolicies);

        var usersCreated = false;

        // Criar usuario admin se nao existir (com tratamento de concorrencia)
        if (!await context.Users.AnyAsync(u => u.Username == "admin"))
        {
            var adminUser = new UserEntity
            {
                Username = "admin",
                Email = "admin@systekna.local",
                PasswordHash = hasher.Hash("admin123"),
                Role = "Admin",
                Policies = adminPolicies,
                IsEmailVerified = true,
                IsApproved = true,
                IsActive = true
            };

            try
            {
                context.Users.Add(adminUser);
                await context.SaveChangesAsync();
                usersCreated = true;
                Console.WriteLine(">>>        admin / admin123 (Admin) - Acesso total");
            }
            catch (DbUpdateException)
            {
                // Usuario ja foi criado por outro processo - ignorar
                context.Entry(adminUser).State = EntityState.Detached;
            }
        }

        // Criar usuario demo se nao existir (com tratamento de concorrencia)
        if (!await context.Users.AnyAsync(u => u.Username == "demo"))
        {
            var demoUser = new UserEntity
            {
                Username = "demo",
                Email = "demo@systekna.local",
                PasswordHash = hasher.Hash("demo123"),
                Role = "User",
                Policies = "PixelDisplay;WbAuth;BaseAccess",
                IsEmailVerified = true,
                IsApproved = true,
                IsActive = true
            };

            try
            {
                context.Users.Add(demoUser);
                await context.SaveChangesAsync();
                usersCreated = true;
                Console.WriteLine(">>>        demo / demo123 (User) - Acesso PixelDisplay e WbAuth");
            }
            catch (DbUpdateException)
            {
                // Usuario ja foi criado por outro processo - ignorar
                context.Entry(demoUser).State = EntityState.Detached;
            }
        }

        // === SEED DE SISTEMAS PADRAO ===
        await SeedDefaultSystemsAsync(context);

        if (usersCreated)
        {
            Console.WriteLine(">>> [SEED] Usuarios criados no banco centralizado.");
        }
        else
        {
            Console.WriteLine(">>> [SEED] Banco ja possui usuarios. Seed ignorado.");
        }
    }

    /// <summary>
    /// Cria sistemas padrao no banco de dados
    /// </summary>
    private static async Task SeedDefaultSystemsAsync(AuthDbContext context)
    {
        var systemsCreated = false;

        // Sistema: PixelDisplay240
        if (!await context.RegisteredSystems.AnyAsync(s => s.SystemCode == "pixeldisplay240"))
        {
            var pixelDisplay = new RegisteredSystem
            {
                SystemCode = "pixeldisplay240",
                DisplayName = "PixelDisplay240 PRO",
                Description = "IDE para desenvolvimento de interfaces embarcadas em displays TFT 240x240",
                BaseUrl = "/",
                IconUrl = "/images/pixeldisplay-icon.png",
                AvailablePolicies = "PixelDisplay;PixelDisplay.AI;PixelDisplay.Export;PixelDisplay.Projects",
                IsActive = true,
                RequiresApproval = false,
                ApiSecret = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32))
            };

            try
            {
                context.RegisteredSystems.Add(pixelDisplay);
                await context.SaveChangesAsync();
                systemsCreated = true;
                Console.WriteLine(">>>        Sistema 'PixelDisplay240 PRO' criado");
            }
            catch (DbUpdateException)
            {
                context.Entry(pixelDisplay).State = EntityState.Detached;
            }
        }

        // Sistema: WbGovAdmin
        if (!await context.RegisteredSystems.AnyAsync(s => s.SystemCode == "wbgovadmin"))
        {
            var govAdmin = new RegisteredSystem
            {
                SystemCode = "wbgovadmin",
                DisplayName = "WB Governance Admin",
                Description = "Painel de administração e governança centralizada",
                BaseUrl = "/admin",
                IconUrl = "/images/admin-icon.png",
                AvailablePolicies = "WbGovAdmin;GovAdmin;BaseAccess",
                IsActive = true,
                RequiresApproval = true,
                ApiSecret = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32))
            };

            try
            {
                context.RegisteredSystems.Add(govAdmin);
                await context.SaveChangesAsync();
                systemsCreated = true;
                Console.WriteLine(">>>        Sistema 'WB Governance Admin' criado");
            }
            catch (DbUpdateException)
            {
                context.Entry(govAdmin).State = EntityState.Detached;
            }
        }

        // Sistema: WbAuth (Autenticação centralizada)
        if (!await context.RegisteredSystems.AnyAsync(s => s.SystemCode == "wbauth"))
        {
            var wbAuth = new RegisteredSystem
            {
                SystemCode = "wbauth",
                DisplayName = "WB Auth Service",
                Description = "Serviço de autenticação e autorização centralizada",
                BaseUrl = "/auth",
                IconUrl = "/images/auth-icon.png",
                AvailablePolicies = "WbAuth;BaseAccess",
                IsActive = true,
                RequiresApproval = false,
                ApiSecret = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32))
            };

            try
            {
                context.RegisteredSystems.Add(wbAuth);
                await context.SaveChangesAsync();
                systemsCreated = true;
                Console.WriteLine(">>>        Sistema 'WB Auth Service' criado");
            }
            catch (DbUpdateException)
            {
                context.Entry(wbAuth).State = EntityState.Detached;
            }
        }

        if (systemsCreated)
        {
            Console.WriteLine(">>> [SEED] Sistemas padrao criados no banco centralizado.");
        }
    }
}

