# 📚 Documentação da Solução PixelDisplay240-Inside

**Versão:** 2.0  
**Framework:** .NET 8  
**Arquitetura:** Clean Architecture com DDD (Domain-Driven Design)  
**Última Atualização:** Junho 2025

---

## 📋 Sumário

1. [Visão Geral da Arquitetura](#visão-geral-da-arquitetura)
2. [Projetos da Solução](#projetos-da-solução)
3. [Systekna.Security (Kernel)](#systekna-security-kernel)
4. [WbGovAdmin (API de Administração)](#wbgovadmin-api-de-administração)
5. [PixelDisplay240 (Aplicação Principal)](#pixeldisplay240-aplicação-principal)
6. [Systekna.Application (Camada de Aplicação)](#systekna-application-camada-de-aplicação)
7. [Fluxo de Autenticação](#fluxo-de-autenticação)
8. [Configurações e Ambiente](#configurações-e-ambiente)
9. [Banco de Dados](#banco-de-dados)
10. [Segurança](#segurança)
11. [Registro de Sistemas Multi-Tenant](#registro-de-sistemas-multi-tenant)
12. [Integração com IA (Gemini)](#integração-com-ia-gemini)

---

## 🏗️ Visão Geral da Arquitetura

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                           SOLUÇÃO PIXELDISPLAY240                            │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  ┌───────────────────────┐    ┌───────────────────────┐                     │
│  │   PixelDisplay240     │    │     WbGovAdmin        │     → Aplicações    │
│  │  (Razor Pages + API)  │    │   (Minimal API)       │       Web           │
│  │  IDE de Prototipagem  │    │   Governança Central  │                     │
│  └───────────────────────┘    └───────────────────────┘                     │
│             │                          │                                     │
│             ▼                          ▼                                     │
│  ┌──────────────────────────────────────────────────────┐                   │
│  │            Systekna.Application                       │    → Camada      │
│  │   (Serviços de Domínio do PixelDisplay240)           │      Aplicação   │
│  │   • PrototypeService  • AIService  • ExportService   │                   │
│  └──────────────────────────────────────────────────────┘                   │
│                         │                                                    │
│                         ▼                                                    │
│  ┌──────────────────────────────────────────────────────┐                   │
│  │             Systekna.Security (Kernel)                │    → Kernel      │
│  │      Autenticação JWT • Governança • Auditoria       │      Core        │
│  │      Rate Limiting • Middlewares de Segurança        │                   │
│  │      Sistema de Registro Multi-Tenant                │                   │
│  └──────────────────────────────────────────────────────┘                   │
│                         │                                                    │
│                         ▼                                                    │
│  ┌──────────────────────────────────────────────────────┐                   │
│  │           Banco de Dados Centralizado                 │    → Persistência│
│  │   SQLite (Debug) │ MySQL (Produção) │ InMemory       │                   │
│  └──────────────────────────────────────────────────────┘                   │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 📁 Projetos da Solução

| Projeto | Tipo | Porta (Dev) | Descrição |
|---------|------|-------------|-----------|
| **Systekna.Security** | Class Library | - | Kernel de segurança com autenticação JWT, governança, auditoria e sistema multi-tenant |
| **WbGovAdmin** | ASP.NET Core Minimal API | 5001 | Painel administrativo para governança de usuários, sistemas e relatórios |
| **PixelDisplay240** | ASP.NET Core Razor Pages | 5000 | IDE web para prototipagem de displays TFT 240x240 com geração de código Arduino/ESP32 |
| **Systekna.Application** | Class Library | - | Camada de aplicação com serviços de domínio do PixelDisplay240 (IA, Export, Prototype) |

---

## 🔐 Systekna.Security (Kernel)

### Descrição
Biblioteca central que fornece toda a infraestrutura de segurança, autenticação, governança e sistema multi-tenant para todos os projetos da solução. Implementa padrões OWASP e proteções contra ataques comuns.

### Estrutura de Pastas
```
AuthSecurity/Kernel/Systekna.Kernel/
├── Application/
│   ├── Services/
│   │   ├── AuthService.cs              # Serviço principal de autenticação
│   │   ├── AuditService.cs             # Serviço de auditoria e logs
│   │   ├── GovernanceService.cs        # Serviço de governança
│   │   ├── PasswordHasher.cs           # Hash de senhas com BCrypt
│   │   ├── PasswordValidationService.cs # Validação de força de senhas
│   │   ├── LoginRateLimiter.cs         # Proteção contra força bruta
│   │   ├── EmailService.cs             # Mock/SMTP para envio de e-mails
│   │   ├── VpsManagerService.cs        # Monitoramento de host/VPS
│   │   └── SystemRegistryService.cs    # CRUD de sistemas multi-tenant
│   └── UseCases/
│       └── AuthUseCases.cs             # Casos de uso de autenticação
├── Domain/
│   ├── DTOs/
│   │   ├── AuthDTOs.cs                 # DTOs de autenticação e sistemas
│   │   └── HostStats.cs                # DTOs de estatísticas de host
│   ├── Entities/
│   │   ├── UserEntity.cs               # Entidade de usuário
│   │   ├── AuditLog.cs                 # Entidade de log de auditoria
│   │   ├── ErrorLog.cs                 # Entidade de log de erros
│   │   ├── SystemSetting.cs            # Configurações do sistema
│   │   └── RegisteredSystem.cs         # Sistemas e UserSystemAccess
│   └── Interfaces/
│       ├── IServices.cs                # Interfaces de serviços
│       └── IRepositories.cs            # Interfaces de repositórios
├── Infrastructure/
│   ├── Data/
│   │   └── AuthDbContext.cs            # DbContext do Entity Framework
│   ├── Repositories/
│   │   └── EfRepositories.cs           # Implementações de repositórios
│   └── Security/
│       ├── SecurityProviders.cs        # JWT Token Provider, Password Hasher
│       ├── SecurityHeadersMiddleware.cs    # Headers de segurança HTTP (OWASP)
│       ├── ThreatDetectionMiddleware.cs    # Detecção de SQL Injection, XSS, Path Traversal
│       ├── GlobalExceptionMiddleware.cs    # Tratamento global de erros
│       ├── ApiCsrfMiddleware.cs            # Proteção CSRF
│       └── SecurityMiddlewareExtensions.cs # Extensions para configuração
└── Extensions/
    └── SysteknaSecurityExtensions.cs   # Métodos de extensão para DI e Seed
```

### Funcionalidades Principais

#### 1. Autenticação (`IAuthService`)
| Método | Descrição |
|--------|-----------|
| `Login` | Autenticação com JWT e refresh token |
| `Register` | Registro de novos usuários com validação de senha |
| `RefreshToken` | Renovação de tokens expirados |
| `ForgotPassword` | Recuperação de senha via e-mail |
| `ResetPassword` | Redefinição de senha com token temporário |
| `ChangePassword` | Alteração de senha pelo usuário autenticado |
| `AdminResetPassword` | Reset de senha pelo administrador |
| `VerifyEmail` | Verificação de e-mail com token |
| `GetAllUsers` | Lista todos os usuários (Admin) |
| `GetPendingUsers` | Lista usuários aguardando aprovação |
| `ApproveUser` | Aprova usuário pendente |
| `UpdateUserRole` | Altera role do usuário |
| `UpdateUserPolicies` | Altera políticas de acesso |
| `DeleteUser` | Remove usuário permanentemente |

#### 2. Governança (`IGovernanceService`)
| Método | Descrição |
|--------|-----------|
| `GetStatsAsync` | Estatísticas do sistema (usuários, sessões, falhas) |
| `ToggleUserStatusAsync` | Bloquear/desbloquear usuários |
| `GetSettingsAsync` | Configurações do sistema |
| `UpdateSettingAsync` | Atualizar configurações |
| `GetRecentErrorsAsync` | Logs de erros recentes |
| `GenerateErrorReportCsvAsync` | Exportar relatório de erros CSV |

#### 3. Auditoria (`IAuditService`)
| Método | Descrição |
|--------|-----------|
| `LogAsync` | Registrar evento de auditoria |
| `GetRecentLogsAsync` | Obter logs recentes |
| `GenerateAuditReportCsvAsync` | Exportar relatório de auditoria CSV |

#### 4. Sistema de Registro Multi-Tenant (`ISystemRegistryService`)
| Método | Descrição |
|--------|-----------|
| `GetAllSystemsAsync` | Lista todos os sistemas cadastrados |
| `GetSystemByIdAsync` | Obtém sistema por ID |
| `GetSystemByCodeAsync` | Obtém sistema por código único |
| `CreateSystemAsync` | Cria novo sistema |
| `UpdateSystemAsync` | Atualiza sistema existente |
| `DeleteSystemAsync` | Remove sistema |
| `ToggleSystemStatusAsync` | Ativa/desativa sistema |
| `RegenerateApiSecretAsync` | Regenera chave API do sistema |
| `GetSystemUsersAsync` | Lista usuários de um sistema |
| `GetPendingAccessRequestsAsync` | Lista solicitações pendentes |
| `GrantAccessAsync` | Concede acesso a usuário |
| `UpdateAccessAsync` | Atualiza acesso existente |
| `RevokeAccessAsync` | Revoga acesso de usuário |
| `ApproveAccessAsync` | Aprova solicitação de acesso |
| `GetUserSystemsAsync` | Lista sistemas do usuário |
| `RequestAccessAsync` | Solicita acesso a sistema |
| `HasAccessAsync` | Verifica se usuário tem acesso |
| `GetUserPoliciesForSystemAsync` | Obtém políticas do usuário no sistema |
| `GetSystemStatsAsync` | Estatísticas de um sistema |
| `GetAllSystemsStatsAsync` | Estatísticas de todos os sistemas |

#### 5. Monitoramento de Host (`IVpsManagerService`)
| Método | Descrição |
|--------|-----------|
| `GetHostStatsAsync` | CPU, memória, disco do servidor |
| `RestartServiceAsync` | Reiniciar serviço do sistema |
| `GetSystemLogsAsync` | Logs do sistema operacional |

### Entidades Principais

#### UserEntity
```csharp
public class UserEntity
{
    public int Id { get; set; }
    public string Username { get; set; }           // Único, 3-100 caracteres
    public string Email { get; set; }              // Único, validado
    public string PasswordHash { get; set; }       // BCrypt
    public string Role { get; set; }               // "User", "Admin"
    public string Policies { get; set; }           // "Policy1;Policy2;..."
    public bool IsEmailVerified { get; set; }      // Confirmação de e-mail
    public string? EmailConfirmationToken { get; set; }
    public string? PasswordResetToken { get; set; }
    public DateTime? ResetTokenExpires { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiryTime { get; set; }
    public bool IsApproved { get; set; }           // Aprovação de governança
    public bool IsActive { get; set; }             // Conta ativa/bloqueada
    public DateTime CreatedAt { get; set; }
}
```

#### RegisteredSystem
```csharp
public class RegisteredSystem
{
    public int Id { get; set; }
    public string SystemCode { get; set; }         // Slug único: "pixeldisplay240"
    public string DisplayName { get; set; }        // Nome amigável
    public string? Description { get; set; }
    public string? BaseUrl { get; set; }           // URL base para redirecionamentos
    public string? IconUrl { get; set; }           // Ícone do sistema
    public string AvailablePolicies { get; set; }  // Políticas disponíveis
    public bool IsActive { get; set; }
    public bool RequiresApproval { get; set; }     // Requer aprovação de admin
    public string? ApiSecret { get; set; }         // Chave de API (Base64)
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public ICollection<UserSystemAccess> UserAccesses { get; set; }
}
```

#### UserSystemAccess
```csharp
public class UserSystemAccess
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int SystemId { get; set; }
    public string SystemRole { get; set; }         // Role específico no sistema
    public string GrantedPolicies { get; set; }    // Políticas concedidas
    public bool IsActive { get; set; }
    public bool IsApproved { get; set; }
    public DateTime RequestedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public int? ApprovedByUserId { get; set; }
    public DateTime? LastAccessAt { get; set; }
    public string? AdminNotes { get; set; }
    public UserEntity? User { get; set; }
    public RegisteredSystem? System { get; set; }
}
```

### Middlewares de Segurança

| Middleware | Função | Configurável |
|------------|--------|--------------|
| `SecurityHeadersMiddleware` | Headers OWASP (X-Frame-Options, CSP, HSTS, etc.) | Sim |
| `ThreatDetectionMiddleware` | Detecta SQL Injection, XSS, Path Traversal, User-Agents suspeitos | Sim |
| `GlobalExceptionMiddleware` | Tratamento centralizado de exceções com logging | - |
| `ApiCsrfMiddleware` | Proteção CSRF para APIs com cookies | Opcional |

### Proteções Implementadas

| Proteção | Implementação | Configuração |
|----------|---------------|--------------|
| **Rate Limiting (Login)** | Bloqueio progressivo após tentativas | 5 tentativas / 15 min |
| **Rate Limiting (API)** | Fixed window por IP | 100 req/min |
| **Validação de Senhas** | Comprimento, maiúsculas, números, especiais | Configurável por ambiente |
| **BCrypt** | Hash de senhas | Work factor 12 |
| **JWT** | Tokens com expiração | 4h (configurável) |
| **Refresh Tokens** | Renovação sem re-login | 7 dias |
| **Auditoria** | Log de ações críticas | Automático |
| **Detecção de Ameaças** | Score de risco por requisição | Bloqueio automático |

---

## 🛡️ WbGovAdmin (API de Administração)

### Descrição
API Minimal de administração para gerenciamento centralizado de usuários, sistemas multi-tenant, monitoramento de host e relatórios. Implementa autenticação JWT com políticas granulares.

### Estrutura
```
AuthSecurity/Web/WbAdmin/
├── Endpoints/
│   ├── AuthEndpoints.cs              # Login/Logout para admins
│   ├── AdminEndpoints.cs             # CRUD completo de usuários
│   ├── SystemRegistryEndpoints.cs    # CRUD de sistemas + acesso usuário-sistema
│   ├── HostEndpoints.cs              # Monitoramento do servidor
│   ├── ReportEndpoints.cs            # Exportação de relatórios CSV
│   ├── PublicEndpoints.cs            # Endpoints públicos (health, version)
│   └── PixelDisplayAdminEndpoints.cs # Endpoints específicos do PixelDisplay
└── Program.cs                        # Configuração da aplicação
```

### Endpoints Disponíveis

#### Autenticação (`/api/auth`)
| Método | Endpoint | Descrição | Auth |
|--------|----------|-----------|------|
| POST | `/login` | Login de administrador | Não |
| POST | `/logout` | Logout | Sim |
| POST | `/refresh` | Renovar token | Sim |

#### Administração de Usuários (`/api/admin`)
| Método | Endpoint | Descrição |
|--------|----------|-----------|
| GET | `/users` | Listar todos os usuários |
| GET | `/users/pending` | Usuários pendentes de aprovação |
| GET | `/users/{id}` | Obter usuário por ID |
| POST | `/users` | Criar novo usuário (já aprovado) |
| POST | `/users/{id}/approve` | Aprovar usuário pendente |
| PUT | `/users/{id}/role` | Alterar role do usuário |
| PUT | `/users/{id}/policies` | Alterar políticas de acesso |
| POST | `/users/{id}/reset-password` | Resetar senha (Admin) |
| POST | `/users/{id}/toggle-status` | Bloquear/desbloquear usuário |
| DELETE | `/users/{id}` | Deletar usuário permanentemente |
| GET | `/stats` | Estatísticas de governança |
| GET | `/logs` | Logs de auditoria |
| GET | `/settings` | Configurações do sistema |
| POST | `/settings` | Atualizar configuração |

#### Sistemas Multi-Tenant (`/api/systems`) - Admin Only
| Método | Endpoint | Descrição |
|--------|----------|-----------|
| GET | `/` | Listar todos os sistemas |
| GET | `/{id}` | Obter sistema por ID |
| GET | `/code/{systemCode}` | Obter sistema por código |
| POST | `/` | Criar novo sistema |
| PUT | `/{id}` | Atualizar sistema |
| DELETE | `/{id}` | Deletar sistema |
| POST | `/{id}/toggle-status` | Ativar/desativar sistema |
| POST | `/{id}/regenerate-secret` | Regenerar chave API |
| GET | `/stats` | Estatísticas de todos os sistemas |
| GET | `/{id}/stats` | Estatísticas de um sistema |
| GET | `/{systemId}/users` | Listar usuários do sistema |
| GET | `/{systemId}/pending` | Solicitações pendentes |
| POST | `/access/grant` | Conceder acesso a usuário |
| PUT | `/access/{accessId}` | Atualizar acesso |
| DELETE | `/access/{accessId}` | Revogar acesso |
| POST | `/access/{accessId}/approve` | Aprovar/rejeitar solicitação |

#### Sistemas do Usuário (`/api/my-systems`) - Autenticado
| Método | Endpoint | Descrição |
|--------|----------|-----------|
| GET | `/` | Listar meus sistemas |
| POST | `/request` | Solicitar acesso a sistema |
| GET | `/check/{systemCode}` | Verificar se tenho acesso |
| GET | `/policies/{systemCode}` | Minhas políticas no sistema |
| GET | `/available` | Sistemas disponíveis para solicitar |

#### Host/VPS (`/api/admin/host`) - Admin Only
| Método | Endpoint | Descrição |
|--------|----------|-----------|
| GET | `/stats` | CPU, memória, disco do servidor |
| GET | `/logs` | Logs do sistema |
| GET | `/errors` | Erros recentes |
| POST | `/restart` | Reiniciar serviço |

#### Relatórios (`/api/admin/reports`) - Admin Only
| Método | Endpoint | Descrição |
|--------|----------|-----------|
| GET | `/errors/export` | Exportar erros (CSV) |
| GET | `/audit/export` | Exportar auditoria (CSV) |

### Políticas de Autorização

```csharp
// Apenas administradores
options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));

// Admin ou usuário com policy específica de governança
options.AddPolicy("GovAdmin", policy => 
    policy.RequireAssertion(context =>
        context.User.IsInRole("Admin") || 
        context.User.HasClaim(c => c.Type == "policy" && 
            (c.Value == "WbGovAdmin" || c.Value == "GovAdmin"))));

// Fallback: todas as rotas requerem AdminOnly
options.FallbackPolicy = options.GetPolicy("AdminOnly");
```

---

## 🎨 PixelDisplay240 (Aplicação Principal)

### Descrição
IDE web completa para desenvolvimento e prototipagem de interfaces para displays TFT 240x240 pixels. Permite criar telas interativas visualmente e exportar código Arduino/ESP32 otimizado para a biblioteca TFT_eSPI.

### Funcionalidades Principais
- **Editor Visual**: Arrastar e soltar elementos no canvas 240x240
- **Múltiplas Telas**: Criar e gerenciar várias telas do projeto
- **Navegação**: Links entre telas para prototipagem de fluxos
- **Assets**: Gerenciamento de imagens e sprites
- **Bindings**: Vincular propriedades a estados dinâmicos
- **IA Generativa**: Gerar pixel art com Google Gemini
- **Auto-Layout**: Otimização de layout com IA
- **Exportação**: Gerar projeto completo para Arduino IDE

### Estrutura
```
Inside/
├── Controllers/
│   ├── HomeController.cs             # Página principal (IDE)
│   └── AccountController.cs          # Páginas de conta
├── Endpoints/
│   ├── AuthEndpoints.cs              # Autenticação de usuários
│   ├── ProfileEndpoints.cs           # Perfil e preferências
│   ├── GovernanceEndpoints.cs        # Governança (Admin no PixelDisplay)
│   └── EndpointExtensions.cs         # Mapeamento centralizado
├── Views/
│   ├── Home/
│   │   ├── Index.cshtml              # IDE principal
│   │   ├── _DesignView.cshtml        # Painel de design
│   │   ├── _PrototypeView.cshtml     # Preview do protótipo
│   │   └── _HardwareView.cshtml      # Configurações de hardware
│   ├── Account/
│   │   ├── Login.cshtml
│   │   ├── Register.cshtml
│   │   ├── ForgotPassword.cshtml
│   │   ├── Profile.cshtml
│   │   ├── Settings.cshtml
│   │   └── AccessDenied.cshtml
│   └── Shared/
│       ├── _Layout.cshtml
│       ├── _Footer.cshtml
│       └── _Modals.cshtml
├── wwwroot/
│   ├── css/
│   ├── js/
│   │   ├── model.js                  # Modelo de dados do projeto
│   │   ├── tft-commands.js           # Comandos TFT_eSPI
│   │   ├── view.js                   # Renderização do canvas
│   │   ├── controller.js             # Lógica de interação
│   │   ├── designer.js               # Editor de elementos
│   │   └── script.js                 # Inicialização
│   └── images/
└── Program.cs                        # Configuração da aplicação
```

### Endpoints da API

#### Autenticação (`/api/auth`)
| Método | Endpoint | Descrição | Auth |
|--------|----------|-----------|------|
| POST | `/login` | Login com JWT | Não |
| POST | `/register` | Registro de novo usuário | Não |
| POST | `/forgot-password` | Solicitar recuperação | Não |
| POST | `/reset-password` | Redefinir senha | Não |
| POST | `/refresh` | Renovar token | Sim |
| GET | `/verify-email` | Verificar e-mail (link) | Não |
| POST | `/logout` | Logout | Sim |
| GET | `/validate` | Validar token atual | Sim |

#### Perfil (`/api/profile`) - Requer `ClienteOuAdmin`
| Método | Endpoint | Descrição |
|--------|----------|-----------|
| GET | `/me` | Dados do usuário logado |
| GET | `/features` | Features disponíveis (AI, Export, etc.) |
| GET | `/access` | Verificar acesso ao PixelDisplay |
| POST | `/change-password` | Alterar senha |
| GET | `/preferences` | Preferências do usuário |
| POST | `/preferences` | Salvar preferências |

#### Governança (`/api/governance`) - Requer `Admin`
| Método | Endpoint | Descrição |
|--------|----------|-----------|
| GET | `/users` | Listar todos os usuários |
| GET | `/users/pending` | Usuários pendentes |
| POST | `/users/{userId}/approve` | Aprovar usuário |
| POST | `/users/{userId}/revoke` | Revogar/restaurar acesso |
| PUT | `/users/{userId}/role` | Alterar role |
| PUT | `/users/{userId}/policies` | Atualizar políticas |
| POST | `/users/{userId}/reset-password` | Resetar senha |
| DELETE | `/users/{userId}` | Excluir usuário |
| GET | `/stats` | Estatísticas de governança |
| GET | `/audit-logs` | Logs de auditoria |
| GET | `/audit-logs/export` | Exportar CSV |

#### API do Protótipo (`/api`) - Requer `ClienteOuAdmin`
| Método | Endpoint | Descrição | Política Extra |
|--------|----------|-----------|----------------|
| GET | `/agents` | Configuração de agentes IA | - |
| POST | `/agents` | Salvar configuração | - |
| POST | `/logs` | Registrar log | - |
| POST | `/config` | Salvar configuração (ex: GeminiKey) | - |
| GET | `/prototype` | Obter projeto atual | - |
| POST | `/prototype/save` | Salvar projeto | - |
| GET | `/prototype/export` | Exportar projeto ZIP | `PixelDisplay.Export` |
| GET | `/ai/image` | Gerar pixel art com IA | `PixelDisplay.AI` |
| POST | `/ai/auto-layout` | Otimizar layout com IA | `PixelDisplay.AI` |

### Políticas de Autorização

```csharp
// Clientes (User) e Administradores podem acessar o PixelDisplay
options.AddPolicy("ClienteOuAdmin", policy => policy.RequireRole("User", "Admin"));
options.AddPolicy("PixelDisplayAccess", policy => policy.RequireRole("User", "Admin"));

// Políticas granulares baseadas em claims do JWT
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
```

### Rate Limiting

```csharp
// API Geral: 100 requisições/minuto por IP
options.AddPolicy("api", ...);

// API de IA: 10 requisições/minuto por IP (mais restritivo)
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
```

---

## 📦 Systekna.Application (Camada de Aplicação)

### Descrição
Camada de aplicação contendo os serviços de domínio específicos do PixelDisplay240, incluindo gerenciamento de projetos, integração com IA e exportação para hardware.

### Estrutura
```
Systekna.Application/
├── Domain/
│   ├── Entities/
│   │   ├── PrototypeProject.cs       # Projeto de prototipagem
│   │   └── AgentConfig.cs            # Configuração de agentes IA
│   └── Aggregates/
│       └── MasterPrototype.cs        # Aggregate root do protótipo
├── DTOs/
│   └── ConfigOptions.cs              # Options pattern para configurações
├── Services/
│   ├── PrototypeService.cs           # Gerenciamento de projetos
│   ├── AIService.cs                  # Integração com Google Gemini
│   ├── HardwareExportService.cs      # Exportação para Arduino/ESP32
│   ├── AgentConfigService.cs         # Configuração de agentes
│   └── LogService.cs                 # Serviço de logs
└── Extensions/
    └── PixelDisplayServiceExtensions.cs  # Registro de serviços no DI
```

### Serviços

#### IPrototypeService
Gerenciamento completo de projetos de prototipagem:

| Método | Descrição |
|--------|-----------|
| `GetProject()` | Obter projeto atual |
| `AddScreen(name, template)` | Adicionar tela (com templates opcionais) |
| `DeleteScreen(id)` | Remover tela |
| `SelectScreen(id)` | Selecionar tela ativa |
| `MoveScreen(id, index)` | Reordenar tela |
| `AddElement(screenId, type, asset)` | Adicionar elemento visual |
| `DeleteElement(screenId, elId)` | Remover elemento |
| `PatchElement(screenId, elId, patch)` | Atualizar propriedades |
| `MoveElement(screenId, elId, index)` | Reordenar elemento |
| `AddAsset(asset)` | Adicionar asset |
| `DeleteAsset(name)` | Remover asset |
| `UpdateScreenBackground(screenId, asset, dataUrl)` | Definir fundo da tela |
| `UpdateScreen(screenId, updates)` | Atualizar propriedades da tela |
| `SaveProject(project)` | Salvar projeto completo |

**Templates de Tela Disponíveis:**
- `loading` - Tela de carregamento com barra de progresso
- `menu` - Menu com header e submenu
- `dashboard` - Dashboard com indicadores

#### IAIService
Integração com Google Gemini para geração de conteúdo:

| Método | Descrição |
|--------|-----------|
| `GeneratePixelArtAsync(apiKey, prompt, seed)` | Gerar pixel art 240x240 |
| `OptimizeLayoutAsync(apiKey, elementsJson, intent)` | Otimizar layout com IA |

**Fluxo de Geração de Pixel Art:**
1. Recebe prompt do usuário
2. Melhora prompt com Gemini 1.5 Flash (opcional)
3. Adiciona instruções de pixel art: "pixel art, 1:1 square, low resolution, limited color palette, crisp edges, no gradients"
4. Envia para Gemini 2.5 Flash Image
5. Extrai bytes da imagem da resposta JSON
6. Retorna PNG pronto para uso

**Otimização de Layout:**
- Usa Gemini 1.5 Flash como assistente de UI/UX
- Respeita grid de 8 pixels
- Evita sobreposição de elementos
- Aplica margens seguras (8px das bordas)
- Otimiza para legibilidade em telas pequenas

#### IHardwareExportService
Exportação de projetos para hardware Arduino/ESP32:

| Método | Descrição |
|--------|-----------|
| `GenerateProjectZip(project)` | Gerar ZIP completo do projeto |
| `GenerateMainCode(project)` | Gerar código .ino principal |
| `GenerateImagesHeader(project)` | Gerar header de imagens PROGMEM |

**Estrutura do ZIP Exportado:**
```
PixelDisplay240_Project.zip
├── PixelDisplay240_Project.ino    # Código principal
├── images.h                       # Declarações de imagens
├── README.md                      # Instruções
└── data/                          # Arquivos para LittleFS
    └── *.jpg                      # Imagens JPEG
```

### Entidades de Domínio

#### PrototypeProject
```csharp
public class PrototypeProject
{
    public List<PrototypeScreen> Screens { get; set; } = new();
    public string ActiveScreenId { get; set; } = "screen_1";
    public string? SelectedElementId { get; set; }
    public List<PrototypeAsset> Assets { get; set; } = new();
    public int ElementSeq { get; set; } = 1;    // Sequencial de elementos
    public int ScreenSeq { get; set; } = 2;     // Sequencial de telas
}
```

#### PrototypeScreen
```csharp
public class PrototypeScreen
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string? Background { get; set; }         // DataURL da imagem
    public string? BackgroundAsset { get; set; }    // Nome do asset
    public string? BackgroundColor { get; set; } = "#000000";
    public List<PrototypeElement> Elements { get; set; } = new();
}
```

#### PrototypeElement
```csharp
public class PrototypeElement
{
    public string Id { get; set; }
    public string Type { get; set; }           // fillRect, drawCircle, etc.
    public string Name { get; set; }           // Label/texto do elemento
    public int X { get; set; } = 10;
    public int Y { get; set; } = 10;
    public int W { get; set; } = 50;
    public int H { get; set; } = 50;
    public string Color { get; set; } = "#38bdf8";
    public string? Asset { get; set; }         // Nome do asset vinculado
    public string? TargetScreenId { get; set; } // Navegação entre telas
    
    // State Logic Bindings para interatividade dinâmica
    public string? XBind { get; set; }
    public string? YBind { get; set; }
    public string? WBind { get; set; }
    public string? HBind { get; set; }
    public string? ColorBind { get; set; }
    public string? ValueBind { get; set; }
}
```

#### PrototypeAsset
```csharp
public class PrototypeAsset
{
    public string Name { get; set; }           // Nome único do asset
    public string DataUrl { get; set; }        // Base64 da imagem
    public int Width { get; set; } = 240;
    public int Height { get; set; } = 240;
    public string Kind { get; set; } = "image"; // image, sprite
    public string StorageType { get; set; } = "flash"; // flash (PROGMEM) ou littlefs
}
```

### Tipos de Elementos Suportados

| Tipo | Descrição | Comando TFT_eSPI | Parâmetros |
|------|-----------|------------------|------------|
| `fillRect` | Retângulo preenchido | `tft.fillRect()` | x, y, w, h, color |
| `drawRect` | Retângulo contornado | `tft.drawRect()` | x, y, w, h, color |
| `fillRoundRect` | Retângulo arredondado | `tft.fillRoundRect()` | x, y, w, h, radius=8, color |
| `fillCircle` | Círculo preenchido | `tft.fillCircle()` | cx, cy, r, color |
| `drawCircle` | Círculo contornado | `tft.drawCircle()` | cx, cy, r, color |
| `fillTriangle` | Triângulo preenchido | `tft.fillTriangle()` | x1, y1, x2, y2, x3, y3, color |
| `fillEllipse` | Elipse preenchida | `tft.fillEllipse()` | cx, cy, rx, ry, color |
| `drawLine` | Linha | `tft.drawLine()` | x1, y1, x2, y2, color |
| `drawFastHLine` | Linha horizontal | `tft.drawFastHLine()` | x, y, w, color |
| `drawFastVLine` | Linha vertical | `tft.drawFastVLine()` | x, y, h, color |
| `drawPixel` | Pixel único | `tft.drawPixel()` | x, y, color |
| `drawString` | Texto | `tft.drawString()` | text, x, y |
| `drawCentreString` | Texto centralizado | `tft.drawCentreString()` | text, cx, y, font |
| `pushImage` | Imagem/sprite | `tft.pushImage()` | x, y, w, h, data |

### Options Pattern

| Classe | Section | Propriedades |
|--------|---------|--------------|
| `AuthOptions` | `Auth` | JwtKey, Issuer, Audience, TokenExpirationHours |
| `FeatureOptions` | `Features` | EnableLogs, EnablePlaceholder, EnableAI, EnableExport |
| `AIOptions` | `AI` | PlaceholderPath, MaxPromptLength, TimeoutSeconds, GeminiModel |
| `RateLimitingOptions` | `RateLimiting` | PermitLimit, WindowSeconds, QueueLimit |
| `CorsOptions` | `Cors` | AllowedOrigins[] |
| `HardwareExportOptions` | `HardwareExport` | DefaultStorageType, MaxAssetSize, IncludeLittleFS |

---

## 🔄 Fluxo de Autenticação

```
┌───────────────┐      POST /api/auth/login       ┌───────────────┐
│   Cliente     │ ──────────────────────────────► │    API        │
│  (Browser)    │   { username, password }        │               │
│               │                                  │               │
│               │ ◄────────────────────────────── │               │
│               │   { token, refreshToken,        │               │
│               │     username, email, role }     │               │
└───────────────┘                                  └───────────────┘
       │                                                  │
       │  Authorization: Bearer <token>                   │
       │  ────────────────────────────────────────►      │
       │                                                  │
       │  API Protected Response                          │
       │  ◄────────────────────────────────────────      │
       │                                                  │
       │  Token Expirado? (401 Unauthorized)              │
       │  POST /api/auth/refresh                          │
       │  { token, refreshToken }                         │
       │  ────────────────────────────────────────►      │
       │                                                  │
       │  Novo { token, refreshToken }                    │
       │  ◄────────────────────────────────────────      │
```

### Claims do Token JWT

| Claim | Tipo | Descrição |
|-------|------|-----------|
| `sub` | `ClaimTypes.NameIdentifier` | ID do usuário (int) |
| `unique_name` | `ClaimTypes.Name` | Username |
| `email` | `ClaimTypes.Email` | E-mail |
| `role` | `ClaimTypes.Role` | Role (User, Admin) |
| `policy` | Custom | Políticas de acesso (múltiplas claims) |
| `jti` | `JwtRegisteredClaimNames.Jti` | ID único do token (GUID) |
| `iat` | `JwtRegisteredClaimNames.Iat` | Data de emissão (Unix timestamp) |

### Configuração JWT

```json
{
  "Jwt": {
    "Key": "sua-chave-secreta-de-pelo-menos-32-caracteres",
    "Issuer": "PixelDisplay240Auth",
    "Audience": "PixelDisplay240Users"
  },
  "Auth": {
    "TokenExpirationHours": 4
  }
}
```

### Fluxo de Registro

```
1. POST /api/auth/register
   ↓
2. Validação de username, email, senha
   ↓
3. Criação do usuário (IsApproved = false em Produção)
   ↓
4. Envio de e-mail de confirmação (SMTP/Mock)
   ↓
5. GET /api/auth/verify-email?token=xxx
   ↓
6. Admin aprova via /api/governance/users/{id}/approve (Produção)
   ↓
7. Usuário pode fazer login
```

### Fluxo de Recuperação de Senha

```
1. POST /api/auth/forgot-password
   ↓
2. Geração de token de reset (GUID, expira em 24h)
   ↓
3. Envio de e-mail com link de reset
   ↓
4. POST /api/auth/reset-password { token, newPassword }
   ↓
5. Validação do token e atualização da senha
```

---

## ⚙️ Configurações e Ambiente

### appsettings.json (PixelDisplay240)

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "App": {
    "BaseUrl": "https://pixeldisplay240.com"
  },
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=db_auth;User Id=app_auth;Password=xxx;",
    "AuthConnection": "Server=localhost;Database=db_auth;User Id=app_auth;Password=xxx;",
    "SqlitePath": "C:\\Users\\...\\AppData\\Local\\Systekna\\CentralAuth.db"
  },
  "Jwt": {
    "Key": "C30EF612-4499-4321-B640-66E77EC02D95",
    "Issuer": "PixelDisplay240Auth",
    "Audience": "PixelDisplay240Users"
  },
  "Auth": {
    "JwtKey": "",
    "Issuer": "PixelDisplay240",
    "Audience": "PixelDisplay240Clients",
    "TokenExpirationHours": 4
  },
  "Features": {
    "EnableLogs": true,
    "EnablePlaceholder": true,
    "EnableAI": true,
    "EnableExport": true,
    "SeedDefaultUser": false
  },
  "AI": {
    "PlaceholderPath": "wwwroot/ai-placeholder.svg",
    "MaxPromptLength": 500,
    "TimeoutSeconds": 60,
    "GeminiModel": "gemini-2.5-flash-image",
    "GeminiTextModel": "gemini-1.5-flash"
  },
  "RateLimiting": {
    "PermitLimit": 100,
    "WindowSeconds": 60,
    "QueueLimit": 10
  },
  "Cors": {
    "AllowedOrigins": ["https://localhost:62565", "https://pixeldisplay240.com"]
  },
  "Smtp": {
    "Host": "smtp.example.com",
    "Port": "587",
    "Username": "",
    "Password": "",
    "EnableSsl": "true",
    "From": "noreply@pixeldisplay240.com"
  }
}
```

### Variáveis de Ambiente

| Variável | Descrição | Padrão |
|----------|-----------|--------|
| `ASPNETCORE_ENVIRONMENT` | Ambiente de execução | Development |
| `Jwt__Key` | Chave secreta JWT | - |
| `ConnectionStrings__AuthConnection` | String de conexão MySQL | - |
| `ConnectionStrings__SqlitePath` | Caminho do SQLite (Debug) | AppData/Local/Systekna |

### Configuração por Ambiente

| Ambiente | Banco | E-mail | Segurança | Seed |
|----------|-------|--------|-----------|------|
| **Development** | SQLite compartilhado | Mock (Console) | Relaxada | Automático |
| **Production** | MySQL centralizado | SMTP real | Estrita | Manual |

### Extensions de Configuração

```csharp
// DEBUG: SQLite compartilhado entre projetos
builder.Services.AddSysteknaSecuritySQLite(configuration, sqlitePath);

// PRODUÇÃO: MySQL centralizado
builder.Services.AddSysteknaSecurityMySQL(configuration, connectionString);

// FALLBACK: InMemory (dados não persistem)
builder.Services.AddSysteknaSecurityInMemory(configuration, "CentralAuthDb");

// Serviços de segurança (middlewares, rate limiter, etc.)
builder.Services.AddSysteknaSecurityServices(isDevelopment);

// Middlewares de segurança na pipeline
app.UseSysteknaSecurityDefaults(environment);      // Padrão
app.UseSysteknaSecurityStrict(environment);        // Estrito
app.UseSysteknaSecurityMiddlewares(environment,    // Customizado
    enableCsrf: true, 
    enableThreatDetection: true);
```

---

## 💾 Banco de Dados

### Providers Suportados

| Ambiente | Provider | Compartilhado | Configuração |
|----------|----------|---------------|--------------|
| **DEBUG** | SQLite | ✅ Sim (arquivo) | `AddSysteknaSecuritySQLite()` |
| **Release** | MySQL (Pomelo) | ✅ Sim (servidor) | `AddSysteknaSecurityMySQL()` |
| **Fallback** | InMemory | ❌ Não | `AddSysteknaSecurityInMemory()` |

### Schema do Banco

#### Tabelas Principais

| Tabela | Descrição | Chave Primária |
|--------|-----------|----------------|
| `Users` | Usuários do sistema | `Id` (int, auto) |
| `AuditLogs` | Logs de auditoria | `Id` (int, auto) |
| `ErrorLogs` | Logs de erros | `Id` (int, auto) |
| `SystemSettings` | Configurações key-value | `Key` (string) |
| `RegisteredSystems` | Sistemas cadastrados | `Id` (int, auto) |
| `UserSystemAccesses` | Relação N:N usuário-sistema | `Id` (int, auto) |

#### Diagrama de Relacionamentos

```
┌─────────────────┐       ┌─────────────────────┐
│     Users       │       │  RegisteredSystems  │
├─────────────────┤       ├─────────────────────┤
│ Id (PK)         │       │ Id (PK)             │
│ Username        │       │ SystemCode          │
│ Email           │       │ DisplayName         │
│ PasswordHash    │       │ AvailablePolicies   │
│ Role            │       │ IsActive            │
│ Policies        │       │ RequiresApproval    │
│ IsActive        │       │ ApiSecret           │
│ IsApproved      │       └─────────────────────┘
└────────┬────────┘                 │
         │                          │
         │    ┌─────────────────────┘
         │    │
         ▼    ▼
┌─────────────────────────┐
│   UserSystemAccesses    │
├─────────────────────────┤
│ Id (PK)                 │
│ UserId (FK → Users)     │
│ SystemId (FK → Systems) │
│ SystemRole              │
│ GrantedPolicies         │
│ IsActive                │
│ IsApproved              │
│ LastAccessAt            │
└─────────────────────────┘
```

### Seed de Dados (DEBUG)

Executado automaticamente via `SeedDefaultUsersAsync()`:

**Usuários:**
```
┌────────────┬────────────┬────────┬─────────────────────────────────────┐
│ Username   │ Senha      │ Role   │ Policies                            │
├────────────┼────────────┼────────┼─────────────────────────────────────┤
│ admin      │ admin123   │ Admin  │ Todas (BaseAccess, WbGovAdmin, etc.)│
│ demo       │ demo123    │ User   │ PixelDisplay;WbAuth;BaseAccess      │
└────────────┴────────────┴────────┴─────────────────────────────────────┘
```

**Sistemas:**
```
┌─────────────────┬─────────────────────────┬───────────────────────┐
│ SystemCode      │ DisplayName             │ RequiresApproval      │
├─────────────────┼─────────────────────────┼───────────────────────┤
│ pixeldisplay240 │ PixelDisplay240 PRO     │ Não                   │
│ wbgovadmin      │ WB Governance Admin     │ Sim                   │
│ wbauth          │ WB Auth Service         │ Não                   │
└─────────────────┴─────────────────────────┴───────────────────────┘
```

### Localização do SQLite (Debug)

```
Windows: %LOCALAPPDATA%\Systekna\CentralAuth.db
Linux:   ~/.local/share/Systekna/CentralAuth.db
macOS:   ~/Library/Application Support/Systekna/CentralAuth.db
```

### Gerenciamento de Schema

```csharp
// No EnsureDatabaseSchemaAsync:
1. Verifica se pode conectar ao banco
2. Se conectou, verifica se tabela RegisteredSystems existe
3. Se não existe, recria o banco (EnsureDeletedAsync + EnsureCreatedAsync)
4. IMPORTANTE: Fecha conexão ANTES de deletar para evitar IOException
```

---

## 🔒 Segurança

### Headers HTTP Implementados (OWASP)

| Header | Valor | Proteção |
|--------|-------|----------|
| `X-Frame-Options` | `DENY` | Clickjacking |
| `X-Content-Type-Options` | `nosniff` | MIME sniffing |
| `X-XSS-Protection` | `1; mode=block` | XSS (navegadores antigos) |
| `Referrer-Policy` | `strict-origin-when-cross-origin` | Referrer leaks |
| `Content-Security-Policy` | (configurável) | XSS, code injection |
| `Permissions-Policy` | (configurável) | APIs sensíveis |
| `Strict-Transport-Security` | `max-age=31536000; includeSubDomains; preload` | HTTPS forçado |
| `Cache-Control` | `no-store, no-cache` | Dados sensíveis em cache |

### Content Security Policy (CSP)

```
default-src 'self';
script-src 'self' 'unsafe-inline' 'unsafe-eval' https://cdnjs.cloudflare.com https://unpkg.com;
style-src 'self' 'unsafe-inline' https://fonts.googleapis.com https://cdnjs.cloudflare.com;
font-src 'self' https://fonts.gstatic.com data:;
img-src 'self' data: blob: https:;
connect-src 'self' https://generativelanguage.googleapis.com;
frame-ancestors 'none';
form-action 'self';
base-uri 'self'
```

### Detecção de Ameaças

| Tipo | Padrões Detectados | Ação |
|------|-------------------|------|
| **SQL Injection** | `union select`, `' or '1'='1`, `'; drop`, `exec(`, etc. | Score +80 |
| **XSS** | `<script`, `javascript:`, `onerror=`, `onclick=`, etc. | Score +70 |
| **Path Traversal** | `../`, `%2e%2e`, `/etc/passwd`, `file://`, etc. | Score +90 |
| **User-Agents** | `sqlmap`, `nikto`, `nmap`, `burp`, `zaproxy`, etc. | Score +50 |
| **Rate Abuse** | > 120 req/min | Score +30 |
| **Rate Severe** | > 360 req/min | Score +50 |
| **Error Flood** | > 10 erros seguidos | Score +40 |

**Níveis de Ameaça:**
- **None** (0-19): Requisição normal
- **Low** (20-39): Monitoramento
- **Medium** (40-69): Log de alerta
- **High** (70-99): Registro detalhado
- **Critical** (100+): Bloqueio automático por 24h

### Rate Limiting

| Contexto | Limite | Janela | Comportamento |
|----------|--------|--------|---------------|
| **Login** | 5 tentativas | 15 min | Bloqueio progressivo |
| **API Geral** | 100 req | 1 min | Fila de 10 |
| **API IA** | 10 req | 1 min | Fila de 2 |

### Validação de Senhas

| Regra | Produção | Desenvolvimento |
|-------|----------|-----------------|
| Mínimo caracteres | 8 | 6 |
| Letra maiúscula | ✅ Obrigatório | ❌ Opcional |
| Letra minúscula | ✅ Obrigatório | ❌ Opcional |
| Número | ✅ Obrigatório | ❌ Opcional |
| Caractere especial | ✅ Obrigatório | ❌ Opcional |

### Proteção CSRF

- **APIs JWT**: Não necessário (stateless)
- **APIs com Cookies**: `ApiCsrfMiddleware` disponível
- **Razor Pages**: `AntiForgeryToken` nativo do ASP.NET Core

### Auditoria

Eventos registrados automaticamente:
- `LOGIN_SUCCESS` / `LOGIN_FAILED` / `LOGIN_DENIED`
- `REGISTER_SUCCESS` / `REGISTER_FAILED`
- `PASSWORD_CHANGED` / `PASSWORD_RESET_REQUESTED` / `PASSWORD_RESET_SUCCESS`
- `USER_APPROVED` / `USER_DEACTIVATED` / `USER_DELETED`
- `USER_ROLE_CHANGED` / `USER_POLICIES_CHANGED`
- `ADMIN_PASSWORD_RESET`
- `VPS_RestartService`
- `Admin_UpdateSetting`

---

## 🏢 Registro de Sistemas Multi-Tenant

### Conceito

O sistema suporta múltiplas aplicações (tenants) que compartilham a mesma base de usuários, mas com permissões granulares por sistema.

### Fluxo de Acesso

```
1. Usuário se registra na plataforma (único cadastro)
   ↓
2. Admin aprova usuário (se RequiresApproval = true)
   ↓
3. Usuário solicita acesso a um sistema específico
   POST /api/my-systems/request { systemId, requestedRole, message }
   ↓
4. Admin do sistema aprova a solicitação
   POST /api/systems/access/{accessId}/approve { approve: true, systemRole, grantedPolicies }
   ↓
5. Usuário recebe acesso com políticas específicas
   ↓
6. No login, token JWT inclui políticas do sistema ativo
```

### Estrutura de Permissões

```
Usuário
├── Role Global: User ou Admin
├── Policies Globais: BaseAccess, WbAuth, etc.
│
└── Acesso por Sistema
    ├── pixeldisplay240
    │   ├── SystemRole: User
    │   └── GrantedPolicies: PixelDisplay.Export;PixelDisplay.AI
    │
    └── wbgovadmin
        ├── SystemRole: Admin
        └── GrantedPolicies: WbGovAdmin;GovAdmin
```

### APIs Self-Service

Os usuários podem gerenciar seus próprios acessos:
- Ver sistemas disponíveis
- Solicitar acesso a novos sistemas
- Ver status de solicitações
- Ver políticas concedidas

---

## 🤖 Integração com IA (Gemini)

### Modelos Utilizados

| Modelo | Uso | Endpoint |
|--------|-----|----------|
| `gemini-1.5-flash` | Melhoria de prompts, otimização de layout | Text generation |
| `gemini-2.5-flash-image` | Geração de pixel art | Image generation |

### Configuração da API Key

A chave da API Gemini é armazenada no arquivo `agent-config.json`:

```json
{
  "Gemini": {
    "ApiKey": "sua-chave-gemini-aqui"
  }
}
```

**Ou via endpoint:**
```
POST /api/config
{ "Key": "GeminiKey", "Value": "sua-chave-gemini-aqui" }
```

### Geração de Pixel Art

**Prompt interno adicionado:**
```
pixel art, 1:1 square, low resolution, limited color palette, crisp edges, no gradients, {prompt do usuário melhorado}
```

**Fluxo:**
1. Recebe prompt do usuário (máx 500 caracteres)
2. Envia para Gemini 1.5 Flash para melhorar o prompt
3. Adiciona instruções de pixel art
4. Envia para Gemini 2.5 Flash Image
5. Extrai bytes PNG da resposta
6. Retorna imagem pronta para uso

### Auto-Layout com IA

O assistente de layout usa o seguinte system prompt:

```
Você é um Senior UI/UX Designer especializado em sistemas embarcados 
e displays TFT de baixa resolução (240x240 pixels).
Sua tarefa é reorganizar elementos UI respeitando:
1. Grid de 8 pixels (coordenadas múltiplas de 8)
2. Não sobrepor elementos importantes
3. Hierarquia visual: elementos principais em destaque
4. Margens seguras de 8 pixels das bordas
5. Otimização para legibilidade em telas pequenas

Retorne APENAS o JSON atualizado, sem explicações.
```

---

## 📝 Notas Adicionais

### Convenções de Código

- **Namespaces**: Seguem a estrutura de pastas
- **DTOs**: Usam `record` para imutabilidade
- **Services**: Injetados via DI (Scoped ou Singleton)
- **Endpoints**: Minimal API com grupos organizados
- **Middleware**: Ordem: Security Headers → Threat Detection → CSRF → Exception Handler

### Dependências Principais

| Pacote | Versão | Uso |
|--------|--------|-----|
| **Microsoft.EntityFrameworkCore** | 8.0.x | ORM |
| **Microsoft.EntityFrameworkCore.Sqlite** | 8.0.x | Provider SQLite |
| **Pomelo.EntityFrameworkCore.MySql** | 8.0.x | Provider MySQL |
| **Microsoft.AspNetCore.Authentication.JwtBearer** | 8.0.x | JWT |
| **BCrypt.Net-Next** | 4.0.x | Hash de senhas |
| **LigerShark.WebOptimizer.Core** | 3.0.x | Bundling JS/CSS |

### Comandos Úteis

```bash
# Executar PixelDisplay240
cd Inside && dotnet run

# Executar WbGovAdmin
cd AuthSecurity/Web/WbAdmin && dotnet run

# Build de todos os projetos
dotnet build

# Limpar e recompilar
dotnet clean && dotnet build
```

---

*Documentação atualizada em Março 2026 - Versão 2.0*
