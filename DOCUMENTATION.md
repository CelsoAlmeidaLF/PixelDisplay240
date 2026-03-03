# ?? Documentação da Solução PixelDisplay240-Inside

**Versão:** 1.0  
**Framework:** .NET 8  
**Arquitetura:** Clean Architecture com DDD (Domain-Driven Design)

---

## ?? Sumário

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

---

## ??? Visão Geral da Arquitetura

```
???????????????????????????????????????????????????????????????????????????????
?                           SOLUÇÃO PIXELDISPLAY240                            ?
???????????????????????????????????????????????????????????????????????????????
?                                                                              ?
?  ???????????????????????    ???????????????????????                         ?
?  ?   PixelDisplay240   ?    ?     WbGovAdmin      ?     ? Aplicações Web    ?
?  ?    (Razor Pages)    ?    ?   (Minimal API)     ?                         ?
?  ???????????????????????    ???????????????????????                         ?
?             ?                          ?                                     ?
?             ?                          ?                                     ?
?  ???????????????????????????????????????????????????                        ?
?  ?            Systekna.Application                  ?    ? Camada Aplicação ?
?  ?   (Serviços de Domínio do PixelDisplay240)      ?                        ?
?  ???????????????????????????????????????????????????                        ?
?                         ?                                                    ?
?                         ?                                                    ?
?  ???????????????????????????????????????????????????                        ?
?  ?             Systekna.Security                    ?    ? Kernel/Core      ?
?  ?      (Autenticação, Segurança, Governança)      ?                        ?
?  ???????????????????????????????????????????????????                        ?
?                         ?                                                    ?
?                         ?                                                    ?
?  ???????????????????????????????????????????????????                        ?
?  ?           Banco de Dados Centralizado            ?    ? Persistência     ?
?  ?   (SQLite Debug / MySQL Produção)               ?                        ?
?  ???????????????????????????????????????????????????                        ?
?                                                                              ?
???????????????????????????????????????????????????????????????????????????????
```

---

## ?? Projetos da Solução

| Projeto | Tipo | Descrição |
|---------|------|-----------|
| **Systekna.Security** | Class Library | Kernel de segurança com autenticação JWT, governança e auditoria |
| **WbGovAdmin** | ASP.NET Core API | Painel administrativo para governança de usuários e sistemas |
| **PixelDisplay240** | ASP.NET Core MVC | Aplicação principal - IDE para prototipagem de displays TFT |
| **Systekna.Application** | Class Library | Camada de aplicação com serviços de domínio do PixelDisplay240 |

---

## ?? Systekna.Security (Kernel)

### Descrição
Biblioteca central que fornece toda a infraestrutura de segurança, autenticação e governança para todos os projetos da solução.

### Estrutura de Pastas
```
AuthSecurity/Kernel/Systekna.Kernel/
??? Application/
?   ??? Services/
?   ?   ??? AuthService.cs           # Serviço principal de autenticação
?   ?   ??? AuditService.cs          # Serviço de auditoria e logs
?   ?   ??? GovernanceService.cs     # Serviço de governança
?   ?   ??? PasswordHasher.cs        # Hash de senhas com BCrypt
?   ?   ??? PasswordValidationService.cs
?   ?   ??? LoginRateLimiter.cs      # Proteção contra força bruta
?   ?   ??? EmailService.cs          # Envio de e-mails (Mock/SMTP)
?   ?   ??? VpsManagerService.cs     # Gerenciamento de host/VPS
?   ?   ??? SystemRegistryService.cs # Cadastro de sistemas
?   ??? UseCases/
?       ??? AuthUseCases.cs          # Casos de uso de autenticação
??? Domain/
?   ??? DTOs/
?   ?   ??? AuthDTOs.cs              # DTOs de autenticação
?   ?   ??? HostStats.cs             # DTOs de estatísticas de host
?   ??? Entities/
?   ?   ??? UserEntity.cs            # Entidade de usuário
?   ?   ??? AuditLog.cs              # Entidade de log de auditoria
?   ?   ??? ErrorLog.cs              # Entidade de log de erros
?   ?   ??? SystemSetting.cs         # Configurações do sistema
?   ?   ??? RegisteredSystem.cs      # Sistemas cadastrados
?   ??? Interfaces/
?       ??? IServices.cs             # Interfaces de serviços
?       ??? IRepositories.cs         # Interfaces de repositórios
??? Infrastructure/
?   ??? Data/
?   ?   ??? AuthDbContext.cs         # DbContext do Entity Framework
?   ??? Repositories/
?   ?   ??? EfRepositories.cs        # Implementações de repositórios
?   ??? Security/
?       ??? SecurityProviders.cs     # JWT Token Provider, Password Hasher
?       ??? SecurityHeadersMiddleware.cs    # Headers de segurança HTTP
?       ??? ThreatDetectionMiddleware.cs    # Detecção de ameaças
?       ??? GlobalExceptionMiddleware.cs    # Tratamento global de erros
?       ??? ApiCsrfMiddleware.cs     # Proteção CSRF
??? Extensions/
    ??? SysteknaSecurityExtensions.cs # Métodos de extensão para DI
```

### Funcionalidades Principais

#### 1. Autenticação (`IAuthService`)
- **Login**: Autenticação com JWT e refresh token
- **Register**: Registro de novos usuários com validação
- **RefreshToken**: Renovação de tokens expirados
- **ForgotPassword**: Recuperação de senha via e-mail
- **ResetPassword**: Redefinição de senha com token
- **ChangePassword**: Alteração de senha pelo usuário
- **AdminResetPassword**: Reset de senha pelo administrador
- **VerifyEmail**: Verificação de e-mail

#### 2. Governança (`IGovernanceService`)
- **GetStatsAsync**: Estatísticas do sistema (usuários, sessões, falhas)
- **ToggleUserStatusAsync**: Bloquear/desbloquear usuários
- **GetSettingsAsync**: Configurações do sistema
- **UpdateSettingAsync**: Atualizar configurações
- **GetRecentErrorsAsync**: Logs de erros recentes
- **GenerateErrorReportCsvAsync**: Exportar relatório de erros

#### 3. Auditoria (`IAuditService`)
- **LogAsync**: Registrar evento de auditoria
- **GetRecentLogsAsync**: Obter logs recentes
- **GenerateAuditReportCsvAsync**: Exportar relatório de auditoria

#### 4. Sistema de Registro (`ISystemRegistryService`)
- CRUD de sistemas cadastrados
- Gerenciamento de acesso usuário-sistema
- Controle de políticas por sistema

### Entidades Principais

#### UserEntity
```csharp
public class UserEntity
{
    public int Id { get; set; }
    public string Username { get; set; }
    public string Email { get; set; }
    public string PasswordHash { get; set; }
    public string Role { get; set; }           // "User", "Admin"
    public string Policies { get; set; }        // "Policy1;Policy2;..."
    public bool IsEmailVerified { get; set; }
    public bool IsApproved { get; set; }
    public bool IsActive { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiryTime { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

#### RegisteredSystem
```csharp
public class RegisteredSystem
{
    public int Id { get; set; }
    public string SystemCode { get; set; }      // Código único (ex: "pixeldisplay240")
    public string DisplayName { get; set; }
    public string? Description { get; set; }
    public string? BaseUrl { get; set; }
    public string AvailablePolicies { get; set; }
    public bool IsActive { get; set; }
    public bool RequiresApproval { get; set; }
    public string ApiSecret { get; set; }
}
```

### Middlewares de Segurança

| Middleware | Função |
|------------|--------|
| `SecurityHeadersMiddleware` | Adiciona headers de segurança (X-Frame-Options, CSP, HSTS) |
| `ThreatDetectionMiddleware` | Detecta SQL Injection, XSS, Path Traversal |
| `GlobalExceptionMiddleware` | Tratamento centralizado de exceções |

### Proteções Implementadas

- **Rate Limiting**: Bloqueio após múltiplas tentativas de login
- **Validação de Senhas**: Requisitos configuráveis de força
- **BCrypt**: Hash de senhas com work factor 12
- **JWT**: Tokens com expiração e refresh tokens
- **Auditoria**: Registro de todas as ações críticas
- **Headers de Segurança**: CSP, X-Frame-Options, HSTS, etc.

---

## ?? WbGovAdmin (API de Administração)

### Descrição
API administrativa para gerenciamento de usuários, sistemas e governança da plataforma.

### Estrutura
```
AuthSecurity/Web/WbAdmin/
??? Endpoints/
?   ??? AuthEndpoints.cs           # Login/Logout para admins
?   ??? AdminEndpoints.cs          # CRUD de usuários
?   ??? SystemRegistryEndpoints.cs # CRUD de sistemas
?   ??? HostEndpoints.cs           # Monitoramento do servidor
?   ??? ReportEndpoints.cs         # Exportação de relatórios
?   ??? PublicEndpoints.cs         # Endpoints públicos
?   ??? PixelDisplayAdminEndpoints.cs
??? Program.cs
```

### Endpoints Disponíveis

#### Autenticação (`/api/auth`)
| Método | Endpoint | Descrição |
|--------|----------|-----------|
| POST | `/login` | Login de administrador |
| POST | `/logout` | Logout |
| POST | `/refresh` | Renovar token |

#### Administração (`/api/admin`)
| Método | Endpoint | Descrição |
|--------|----------|-----------|
| GET | `/users` | Listar todos os usuários |
| GET | `/users/pending` | Usuários pendentes de aprovação |
| GET | `/users/{id}` | Obter usuário por ID |
| POST | `/users` | Criar novo usuário |
| POST | `/users/{id}/approve` | Aprovar usuário |
| PUT | `/users/{id}/role` | Alterar role do usuário |
| PUT | `/users/{id}/policies` | Alterar políticas |
| POST | `/users/{id}/reset-password` | Resetar senha |
| POST | `/users/{id}/toggle-status` | Bloquear/desbloquear |
| DELETE | `/users/{id}` | Deletar usuário |
| GET | `/stats` | Estatísticas do sistema |
| GET | `/logs` | Logs de auditoria |
| GET | `/settings` | Configurações |
| POST | `/settings` | Atualizar configuração |

#### Sistemas (`/api/systems`)
| Método | Endpoint | Descrição |
|--------|----------|-----------|
| GET | `/` | Listar sistemas |
| GET | `/{id}` | Obter sistema por ID |
| POST | `/` | Criar sistema |
| PUT | `/{id}` | Atualizar sistema |
| DELETE | `/{id}` | Deletar sistema |
| POST | `/{id}/toggle-status` | Ativar/desativar |
| GET | `/{id}/users` | Usuários do sistema |
| POST | `/access/grant` | Conceder acesso |

#### Host (`/api/admin/host`)
| Método | Endpoint | Descrição |
|--------|----------|-----------|
| GET | `/stats` | Estatísticas do servidor |
| GET | `/logs` | Logs do sistema |
| GET | `/errors` | Erros recentes |
| POST | `/restart` | Reiniciar serviço |

#### Relatórios (`/api/admin/reports`)
| Método | Endpoint | Descrição |
|--------|----------|-----------|
| GET | `/errors/export` | Exportar erros (CSV) |
| GET | `/audit/export` | Exportar auditoria (CSV) |

### Políticas de Autorização

```csharp
// Apenas administradores
options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));

// Admin ou usuário com policy específica
options.AddPolicy("GovAdmin", policy => 
    policy.RequireAssertion(context =>
        context.User.IsInRole("Admin") || 
        context.User.HasClaim(c => c.Type == "policy" && 
            (c.Value == "WbGovAdmin" || c.Value == "GovAdmin"))));
```

---

## ?? PixelDisplay240 (Aplicação Principal)

### Descrição
IDE web para desenvolvimento e prototipagem de interfaces para displays TFT 240x240 pixels, com geração de código Arduino/ESP32.

### Estrutura
```
Inside/
??? Controllers/
?   ??? HomeController.cs          # Página principal
?   ??? AccountController.cs       # Páginas de conta
??? Endpoints/
?   ??? AuthEndpoints.cs           # Autenticação
?   ??? ProfileEndpoints.cs        # Perfil do usuário
?   ??? GovernanceEndpoints.cs     # Governança
?   ??? EndpointExtensions.cs      # Mapeamento de endpoints
??? Views/
?   ??? Home/
?   ?   ??? Index.cshtml           # Página principal
?   ??? Account/
?   ?   ??? Login.cshtml
?   ?   ??? Register.cshtml
?   ?   ??? ForgotPassword.cshtml
?   ?   ??? Profile.cshtml
?   ?   ??? Settings.cshtml
?   ??? Shared/
?       ??? _Layout.cshtml
?       ??? _Footer.cshtml
??? wwwroot/
?   ??? css/
?   ??? js/
?   ?   ??? model.js
?   ?   ??? view.js
?   ?   ??? controller.js
?   ?   ??? designer.js
?   ?   ??? script.js
?   ??? images/
??? Program.cs
```

### Endpoints da API

#### Autenticação (`/api/auth`)
| Método | Endpoint | Descrição |
|--------|----------|-----------|
| POST | `/login` | Login |
| POST | `/register` | Registro |
| POST | `/forgot-password` | Esqueci senha |
| POST | `/reset-password` | Redefinir senha |
| POST | `/refresh` | Renovar token |
| GET | `/verify-email` | Verificar e-mail |
| POST | `/logout` | Logout |
| GET | `/validate` | Validar token |

#### Perfil (`/api/profile`)
| Método | Endpoint | Descrição |
|--------|----------|-----------|
| GET | `/me` | Dados do usuário logado |
| GET | `/features` | Features disponíveis |
| GET | `/access` | Verificar acesso |
| POST | `/change-password` | Alterar senha |
| GET | `/preferences` | Preferências |
| POST | `/preferences` | Salvar preferências |

#### Protótipo (`/api`)
| Método | Endpoint | Descrição |
|--------|----------|-----------|
| GET | `/agents` | Obter configuração de agentes |
| POST | `/agents` | Salvar configuração |
| POST | `/logs` | Registrar log |
| POST | `/config` | Salvar configuração |
| GET | `/prototype` | Obter projeto atual |
| POST | `/prototype/save` | Salvar projeto |
| GET | `/prototype/export` | Exportar projeto (ZIP) |

#### IA (`/api`)
| Método | Endpoint | Descrição |
|--------|----------|-----------|
| GET | `/ai/image` | Gerar pixel art com IA |
| POST | `/ai/auto-layout` | Otimizar layout com IA |

### Políticas de Autorização

```csharp
// Clientes e administradores
options.AddPolicy("ClienteOuAdmin", policy => policy.RequireRole("User", "Admin"));

// Políticas granulares
options.AddPolicy("PixelDisplay.AI", policy => 
    policy.RequireAssertion(context =>
        context.User.IsInRole("Admin") || 
        context.User.HasClaim(c => c.Type == "policy" && c.Value == "PixelDisplay.AI")));

options.AddPolicy("PixelDisplay.Export", policy => ...);
options.AddPolicy("PixelDisplay.Projects", policy => ...);
```

---

## ?? Systekna.Application (Camada de Aplicação)

### Descrição
Camada de aplicação contendo os serviços de domínio específicos do PixelDisplay240.

### Estrutura
```
Systekna.Application/
??? Domain/
?   ??? Entities/
?   ?   ??? PrototypeProject.cs    # Projeto de prototipagem
?   ?   ??? AgentConfig.cs         # Configuração de agentes
?   ??? Aggregates/
?       ??? MasterPrototype.cs     # Aggregate root do protótipo
??? DTOs/
?   ??? ConfigOptions.cs           # Options pattern para configurações
??? Services/
?   ??? PrototypeService.cs        # Serviço de prototipagem
?   ??? AIService.cs               # Integração com Gemini AI
?   ??? HardwareExportService.cs   # Exportação para Arduino/ESP32
?   ??? AgentConfigService.cs      # Gerenciamento de configuração
?   ??? LogService.cs              # Serviço de logs
??? Extensions/
    ??? PixelDisplayServiceExtensions.cs
```

### Serviços

#### IPrototypeService
Gerenciamento de projetos de prototipagem:
- `GetProject()`: Obter projeto atual
- `AddScreen()`: Adicionar tela
- `DeleteScreen()`: Remover tela
- `SelectScreen()`: Selecionar tela ativa
- `AddElement()`: Adicionar elemento visual
- `DeleteElement()`: Remover elemento
- `PatchElement()`: Atualizar propriedades do elemento
- `AddAsset()`: Adicionar asset (imagem/sprite)
- `SaveProject()`: Salvar projeto

#### IAIService
Integração com Google Gemini:
- `GeneratePixelArtAsync()`: Gerar pixel art a partir de prompt
- `OptimizeLayoutAsync()`: Otimizar layout de elementos com IA

#### IHardwareExportService
Exportação de projetos:
- `GenerateProjectZip()`: Gerar ZIP com código Arduino
- `GenerateMainCode()`: Gerar código .ino principal
- `GenerateImagesHeader()`: Gerar header de imagens PROGMEM

### Entidades de Domínio

#### PrototypeProject
```csharp
public class PrototypeProject
{
    public List<PrototypeScreen> Screens { get; set; }
    public string ActiveScreenId { get; set; }
    public string? SelectedElementId { get; set; }
    public List<PrototypeAsset> Assets { get; set; }
    public int ElementSeq { get; set; }
    public int ScreenSeq { get; set; }
}
```

#### PrototypeScreen
```csharp
public class PrototypeScreen
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string? Background { get; set; }
    public string? BackgroundAsset { get; set; }
    public string? BackgroundColor { get; set; }
    public List<PrototypeElement> Elements { get; set; }
}
```

#### PrototypeElement
```csharp
public class PrototypeElement
{
    public string Id { get; set; }
    public string Type { get; set; }        // fillRect, drawCircle, drawString, etc.
    public string Name { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public int W { get; set; }
    public int H { get; set; }
    public string Color { get; set; }
    public string? Asset { get; set; }
    public string? TargetScreenId { get; set; }
    
    // Bindings para lógica de estado
    public string? XBind { get; set; }
    public string? YBind { get; set; }
    public string? ColorBind { get; set; }
    public string? ValueBind { get; set; }
}
```

### Tipos de Elementos Suportados

| Tipo | Descrição | Comando TFT_eSPI |
|------|-----------|------------------|
| `fillRect` | Retângulo preenchido | `tft.fillRect()` |
| `drawRect` | Retângulo contornado | `tft.drawRect()` |
| `fillRoundRect` | Retângulo arredondado | `tft.fillRoundRect()` |
| `fillCircle` | Círculo preenchido | `tft.fillCircle()` |
| `drawCircle` | Círculo contornado | `tft.drawCircle()` |
| `fillTriangle` | Triângulo preenchido | `tft.fillTriangle()` |
| `fillEllipse` | Elipse preenchida | `tft.fillEllipse()` |
| `drawLine` | Linha | `tft.drawLine()` |
| `drawFastHLine` | Linha horizontal | `tft.drawFastHLine()` |
| `drawFastVLine` | Linha vertical | `tft.drawFastVLine()` |
| `drawPixel` | Pixel único | `tft.drawPixel()` |
| `drawString` | Texto | `tft.drawString()` |
| `drawCentreString` | Texto centralizado | `tft.drawCentreString()` |
| `pushImage` | Imagem/sprite | `tft.pushImage()` |

---

## ?? Fluxo de Autenticação

```
???????????????      POST /api/auth/login       ???????????????
?   Cliente   ? ??????????????????????????????  ?    API      ?
?  (Browser)  ?                                  ?             ?
?             ?  ?????????????????????????????? ?             ?
?             ?    { token, refreshToken }      ?             ?
???????????????                                  ???????????????
       ?                                               ?
       ?  Authorization: Bearer <token>                ?
       ?  ????????????????????????????????????????    ?
       ?                                               ?
       ?  API Protected Response                       ?
       ?  ????????????????????????????????????????    ?
       ?                                               ?
       ?  Token Expirado?                              ?
       ?  POST /api/auth/refresh                       ?
       ?  { token, refreshToken }                      ?
       ?  ????????????????????????????????????????    ?
       ?                                               ?
       ?  Novo { token, refreshToken }                 ?
       ?  ????????????????????????????????????????    ?
```

### Claims do Token JWT

| Claim | Descrição |
|-------|-----------|
| `sub` | ID do usuário |
| `unique_name` | Username |
| `email` | E-mail |
| `role` | Role (User, Admin) |
| `policy` | Políticas de acesso (múltiplas) |
| `jti` | ID único do token |
| `iat` | Data de emissão |

---

## ?? Configurações e Ambiente

### appsettings.json

```json
{
  "ConnectionStrings": {
    "AuthConnection": "Server=localhost;Database=SysteknaAuth;...",
    "SqlitePath": "C:\\Users\\...\\Systekna\\CentralAuth.db"
  },
  "Jwt": {
    "Key": "sua-chave-secreta-de-pelo-menos-32-caracteres",
    "Issuer": "PixelDisplay240",
    "Audience": "PixelDisplay240Clients",
    "ExpirationHours": 4
  },
  "Features": {
    "EnableLogs": true,
    "EnablePlaceholder": true,
    "EnableAI": true,
    "EnableExport": true,
    "SeedDefaultUser": true
  },
  "AI": {
    "PlaceholderPath": "wwwroot/ai-placeholder.svg",
    "MaxPromptLength": 500,
    "TimeoutSeconds": 60,
    "GeminiModel": "gemini-2.5-flash-image"
  },
  "RateLimiting": {
    "PermitLimit": 100,
    "WindowSeconds": 60,
    "QueueLimit": 10
  },
  "Cors": {
    "AllowedOrigins": ["http://localhost:5000", "https://seudominio.com"]
  }
}
```

### Options Pattern

| Classe | Section | Descrição |
|--------|---------|-----------|
| `AuthOptions` | `Auth` | Configurações JWT |
| `FeatureOptions` | `Features` | Feature toggles |
| `AIOptions` | `AI` | Configurações de IA |
| `RateLimitingOptions` | `RateLimiting` | Rate limiting |
| `CorsOptions` | `Cors` | CORS |

---

## ?? Banco de Dados

### Providers Suportados

| Ambiente | Provider | Compartilhado |
|----------|----------|---------------|
| DEBUG | SQLite | ? Sim (arquivo compartilhado) |
| Release | MySQL | ? Sim (servidor centralizado) |
| Fallback | InMemory | ? Não |

### Tabelas

| Tabela | Descrição |
|--------|-----------|
| `Users` | Usuários do sistema |
| `AuditLogs` | Logs de auditoria |
| `ErrorLogs` | Logs de erros |
| `SystemSettings` | Configurações do sistema |
| `RegisteredSystems` | Sistemas cadastrados |
| `UserSystemAccesses` | Relação usuário-sistema |

### Seed de Dados (DEBUG)

```
Usuários criados automaticamente:
- admin / admin123 (Role: Admin) - Acesso total
- demo / demo123 (Role: User) - Acesso básico

Sistemas criados automaticamente:
- pixeldisplay240 (PixelDisplay240 PRO)
- wbgovadmin (WB Governance Admin)
- wbauth (WB Auth Service)
```

---

## ??? Segurança

### Headers HTTP Implementados

| Header | Valor | Proteção |
|--------|-------|----------|
| `X-Frame-Options` | `DENY` | Clickjacking |
| `X-Content-Type-Options` | `nosniff` | MIME sniffing |
| `X-XSS-Protection` | `1; mode=block` | XSS |
| `Referrer-Policy` | `strict-origin-when-cross-origin` | Referrer leaks |
| `Content-Security-Policy` | (configurável) | XSS, injection |
| `Strict-Transport-Security` | `max-age=31536000` | HTTPS |

### Detecção de Ameaças

Padrões detectados automaticamente:
- **SQL Injection**: `union select`, `' or '1'='1`, etc.
- **XSS**: `<script`, `javascript:`, `onerror=`, etc.
- **Path Traversal**: `../`, `%2e%2e`, `/etc/passwd`, etc.
- **User-Agents Suspeitos**: `sqlmap`, `nikto`, `burp`, etc.

### Rate Limiting

| Cenário | Política |
|---------|----------|
| Login | 5 tentativas / 15 min (bloqueio progressivo) |
| API Geral | 100 req/min por IP |
| API IA | 10 req/min por IP |

### Validação de Senhas

| Política | Produção | Desenvolvimento |
|----------|----------|-----------------|
| Mínimo caracteres | 8 | 6 |
| Letra maiúscula | ? | ? |
| Letra minúscula | ? | ? |
| Número | ? | ? |
| Caractere especial | ? | ? |

---

## ?? Notas Adicionais

### Convenções de Código

- **Namespaces**: Seguem a estrutura de pastas
- **DTOs**: Usam records para imutabilidade
- **Services**: Injetados via DI (Scoped ou Singleton conforme necessário)
- **Endpoints**: Minimal API com grupos organizados por funcionalidade

### Dependências Principais

- **Microsoft.EntityFrameworkCore** (8.0.2) - ORM
- **Microsoft.AspNetCore.Authentication.JwtBearer** (8.0.2) - JWT
- **BCrypt.Net-Next** (4.0.3) - Hash de senhas
- **Pomelo.EntityFrameworkCore.MySql** (8.0.2) - MySQL
- **LigerShark.WebOptimizer.Core** (3.0.405) - Bundling JS/CSS

---

*Documentação gerada automaticamente em {DATA}*
