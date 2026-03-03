# ?? Documentação de Implementações Sugeridas

**Versão:** 2.1  
**Data:** Janeiro 2025  
**Status:** Planejamento e Especificação

---

## ?? Índice

1. [Resumo das Correções Implementadas](#correções-implementadas)
2. [Funcionalidades Sugeridas - Alta Prioridade](#alta-prioridade)
3. [Funcionalidades Sugeridas - Média Prioridade](#média-prioridade)
4. [Funcionalidades Sugeridas - Baixa Prioridade](#baixa-prioridade)
5. [Especificações Técnicas Detalhadas](#especificações-técnicas)
6. [Roadmap de Implementação](#roadmap)

---

## ? Correções Implementadas

As seguintes correções foram aplicadas nesta versão:

### 1. Validação de Input no Endpoint `/api/config`

**Arquivo:** `Inside/Program.cs`

**Problema:** O endpoint aceitava qualquer chave de configuração sem validação.

**Solução:**
- Implementada whitelist de chaves permitidas (`GeminiKey`, `Theme`, `Language`, `AutoSave`)
- Adicionada validação de tamanho máximo do valor (1024 caracteres)
- Logging de tentativas de configurar chaves não permitidas
- Mensagens de erro descritivas

```csharp
var allowedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
{
    "GeminiKey", "Theme", "Language", "AutoSave"
};

if (!allowedKeys.Contains(key))
{
    logger.LogWarning("Tentativa de configurar chave não permitida: {Key}", key);
    return Results.BadRequest(new { message = $"Chave '{key}' não é permitida." });
}
```

### 2. Rate Limiting para Exportação

**Arquivo:** `Inside/Program.cs`

**Problema:** O endpoint de exportação não tinha rate limiting específico.

**Solução:**
- Nova política `export` com limite de 5 requisições por minuto
- Validação de projeto vazio antes da exportação
- Logging de exportações

```csharp
options.AddPolicy("export", httpContext =>
    RateLimitPartition.GetFixedWindowLimiter(
        partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
        factory: _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 5,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 1
        }));
```

### 3. Substituição de Console.WriteLine por ILogger

**Arquivos afetados:**
- `AuthService.cs`
- `AIService.cs`
- `PrototypeService.cs`
- `HardwareExportService.cs`

**Problema:** Uso de `Console.WriteLine` em código de produção.

**Solução:** Injeção de `ILogger<T>` em todos os serviços com logging estruturado.

### 4. Validação de Tamanho de Assets

**Arquivo:** `HardwareExportService.cs`

**Problema:** Assets sem limite de tamanho podiam causar problemas de memória.

**Solução:**
- Limite máximo de 1 MB por asset
- Limite de 50 assets por projeto
- Validação com logging de warnings para assets ignorados

### 5. Modelo AgentConfig Expandido

**Arquivo:** `AgentConfig.cs`

**Novas propriedades:**
- `Theme` (dark/light)
- `Language` (pt-BR, en-US, es-ES)
- `AutoSave` (boolean)
- `GeminiConfig.ImageModel`
- `GeminiConfig.TextModel`

### 6. README do Projeto Principal

**Arquivo:** `Inside/README.md`

Documentação completa do projeto PixelDisplay240 com instruções de uso.

---

## ?? Alta Prioridade

### 1. Persistência de Projetos por Usuário

**Objetivo:** Salvar projetos no banco de dados vinculados ao usuário autenticado.

**Entidades:**

```csharp
public class UserProject
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Name { get; set; } = "Novo Projeto";
    public string? Description { get; set; }
    public string ProjectJson { get; set; } = "{}";  // Serializado
    public string? ThumbnailBase64 { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public bool IsPublic { get; set; } = false;
    public bool IsArchived { get; set; } = false;
    
    public UserEntity? User { get; set; }
}
```

**Endpoints:**

| Método | Endpoint | Descrição |
|--------|----------|-----------|
| GET | `/api/projects` | Listar projetos do usuário |
| GET | `/api/projects/{id}` | Obter projeto específico |
| POST | `/api/projects` | Criar novo projeto |
| PUT | `/api/projects/{id}` | Atualizar projeto |
| DELETE | `/api/projects/{id}` | Excluir projeto |
| POST | `/api/projects/{id}/duplicate` | Duplicar projeto |
| GET | `/api/projects/public` | Listar projetos públicos |

**Migração de Dados:**
- Adicionar tabela `UserProjects` no `AuthDbContext`
- Mover dados de arquivo para banco
- Manter compatibilidade com projetos locais

**Estimativa:** 2-3 semanas

---

### 2. Sincronização em Tempo Real (SignalR)

**Objetivo:** Permitir edição colaborativa e sincronização entre dispositivos.

**Hub SignalR:**

```csharp
public class ProjectHub : Hub
{
    public async Task JoinProject(string projectId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, projectId);
    }

    public async Task LeaveProject(string projectId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, projectId);
    }

    public async Task BroadcastChange(string projectId, ProjectChangeDto change)
    {
        await Clients.OthersInGroup(projectId).SendAsync("ReceiveChange", change);
    }

    public async Task BroadcastCursor(string projectId, CursorPositionDto position)
    {
        await Clients.OthersInGroup(projectId).SendAsync("ReceiveCursor", position);
    }
}
```

**DTOs:**

```csharp
public record ProjectChangeDto(
    string ChangeType,      // "AddElement", "UpdateElement", "DeleteElement", etc.
    string? TargetId,       // ID do elemento/tela afetado
    JsonElement? Data,      // Dados da mudança
    DateTime Timestamp
);

public record CursorPositionDto(
    string UserId,
    string Username,
    int X,
    int Y,
    string? SelectedElementId
);
```

**Estimativa:** 3-4 semanas

---

### 3. Preview em Dispositivo Real

**Objetivo:** Permitir que ESP32/Arduino conecte e receba atualizações ao vivo.

**Componentes:**

1. **WebSocket Server** para dispositivos
2. **Protocolo binário** para comunicação eficiente
3. **SDK Arduino** para conexão

**Protocolo:**

```
HEADER (4 bytes):
  [0-1] Magic: 0xPD40
  [2]   Command: DRAW_RECT, DRAW_CIRCLE, DRAW_TEXT, etc.
  [3]   Length: Tamanho do payload

PAYLOAD (variável):
  Dados específicos do comando
```

**Endpoint:**

```csharp
app.MapGet("/ws/device", async (HttpContext context, IPrototypeService service) =>
{
    if (context.WebSockets.IsWebSocketRequest)
    {
        var webSocket = await context.WebSockets.AcceptWebSocketAsync();
        await HandleDeviceConnection(webSocket, service);
    }
    else
    {
        context.Response.StatusCode = 400;
    }
});
```

**Estimativa:** 4-6 semanas

---

### 4. Biblioteca de Templates e Componentes

**Objetivo:** Oferecer templates pré-definidos e componentes reutilizáveis.

**Estrutura:**

```csharp
public class ComponentLibrary
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Category { get; set; }  // "Gauge", "Button", "Chart", "Icon"
    public string ComponentJson { get; set; }
    public string? ThumbnailBase64 { get; set; }
    public bool IsBuiltIn { get; set; }
    public int? CreatedByUserId { get; set; }
    public int Downloads { get; set; }
    public double Rating { get; set; }
}
```

**Categorias:**

| Categoria | Exemplos |
|-----------|----------|
| **Gauges** | Velocímetro, termômetro, medidor de bateria |
| **Buttons** | Botões estilizados, toggles, radio buttons |
| **Charts** | Gráfico de barras, linha, pizza mini |
| **Icons** | Ícones de 16x16 e 32x32 pixels |
| **Widgets** | Relógio, calendário, weather widget |
| **Layouts** | Headers, footers, grids |

**Endpoints:**

| Método | Endpoint | Descrição |
|--------|----------|-----------|
| GET | `/api/components` | Listar componentes |
| GET | `/api/components/{id}` | Obter componente |
| POST | `/api/components` | Criar componente |
| POST | `/api/components/{id}/use` | Adicionar ao projeto |

**Estimativa:** 3-4 semanas

---

## ?? Média Prioridade

### 5. Dashboard de Analytics

**Objetivo:** Fornecer métricas de uso do sistema.

**Métricas:**

```csharp
public class SystemAnalytics
{
    public int TotalProjects { get; set; }
    public int ProjectsCreatedToday { get; set; }
    public int ProjectsExportedToday { get; set; }
    public int AIGenerationsToday { get; set; }
    public int ActiveUsersToday { get; set; }
    public Dictionary<string, int> ElementTypeUsage { get; set; }
    public Dictionary<string, int> FeatureUsage { get; set; }
    public List<PopularProjectDto> PopularProjects { get; set; }
}
```

**Endpoints:**

| Método | Endpoint | Descrição |
|--------|----------|-----------|
| GET | `/api/analytics/overview` | Visão geral |
| GET | `/api/analytics/usage` | Uso por período |
| GET | `/api/analytics/popular` | Projetos populares |
| GET | `/api/analytics/ai` | Estatísticas de IA |

**Estimativa:** 2 semanas

---

### 6. Integração com Plataformas IoT

**Objetivo:** Exportar para diferentes plataformas além de Arduino.

**Plataformas Suportadas:**

| Plataforma | Formato | Descrição |
|------------|---------|-----------|
| **PlatformIO** | `platformio.ini` + código | Projeto completo |
| **ESP-IDF** | CMakeLists.txt + código | Projeto nativo ESP |
| **MicroPython** | `.py` | Scripts Python |
| **LVGL** | Código C + assets | Biblioteca gráfica |

**Interface:**

```csharp
public interface IPlatformExporter
{
    string PlatformName { get; }
    byte[] Export(PrototypeProject project, ExportOptions options);
}

public class ExportOptions
{
    public string TargetBoard { get; set; } = "ESP32";
    public string DisplayDriver { get; set; } = "ST7789";
    public int DisplayWidth { get; set; } = 240;
    public int DisplayHeight { get; set; } = 240;
    public bool IncludeWiFi { get; set; } = false;
    public bool IncludeBluetooth { get; set; } = false;
}
```

**Estimativa:** 4-6 semanas (por plataforma)

---

### 7. Sistema de Plugins/Extensões

**Objetivo:** Permitir extensibilidade do sistema.

**Arquitetura:**

```csharp
public interface IPixelDisplayPlugin
{
    string Id { get; }
    string Name { get; }
    string Version { get; }
    
    void Initialize(IPluginContext context);
    void RegisterElementTypes(IElementTypeRegistry registry);
    void RegisterExporters(IExporterRegistry registry);
}

public interface IPluginContext
{
    IServiceProvider Services { get; }
    ILogger Logger { get; }
    IConfiguration Configuration { get; }
}
```

**Manifest do Plugin:**

```json
{
  "id": "custom-gauges",
  "name": "Custom Gauges Plugin",
  "version": "1.0.0",
  "author": "Author Name",
  "description": "Adds custom gauge elements",
  "entryPoint": "CustomGauges.dll"
}
```

**Estimativa:** 6-8 semanas

---

### 8. Exportação em Múltiplos Formatos

**Objetivo:** Oferecer mais opções de exportação.

**Formatos:**

| Formato | Descrição | Uso |
|---------|-----------|-----|
| **PNG/SVG** | Imagem estática da tela | Documentação, preview |
| **GIF** | Animação de navegação | Demonstração |
| **HTML** | Simulador web standalone | Prototipagem |
| **PDF** | Documentação completa | Especificação |

**Endpoint:**

```csharp
api.MapGet("/prototype/export/{format}", async (
    string format, 
    IPrototypeService service,
    IExportService exportService) =>
{
    var project = service.GetProject();
    
    return format.ToLower() switch
    {
        "png" => Results.File(exportService.ToPng(project), "image/png"),
        "svg" => Results.File(exportService.ToSvg(project), "image/svg+xml"),
        "gif" => Results.File(exportService.ToGif(project), "image/gif"),
        "html" => Results.File(exportService.ToHtml(project), "text/html"),
        "pdf" => Results.File(exportService.ToPdf(project), "application/pdf"),
        _ => Results.BadRequest($"Formato '{format}' não suportado")
    };
});
```

**Estimativa:** 3-4 semanas

---

## ?? Baixa Prioridade

### 9. Internacionalização (i18n)

**Objetivo:** Suporte a múltiplos idiomas.

**Idiomas Planejados:**
- Português (pt-BR) - padrão
- Inglês (en-US)
- Espanhol (es-ES)

**Implementação:**
- Usar `IStringLocalizer<T>` do ASP.NET Core
- Arquivos de recurso `.resx`
- Detecção automática de idioma via header `Accept-Language`

**Estimativa:** 2 semanas

---

### 10. Tutoriais Interativos

**Objetivo:** Guiar novos usuários com tutoriais passo a passo.

**Componentes:**

```csharp
public class Tutorial
{
    public string Id { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public List<TutorialStep> Steps { get; set; }
    public string Category { get; set; }  // "Beginner", "Advanced"
    public int DurationMinutes { get; set; }
}

public class TutorialStep
{
    public int Order { get; set; }
    public string Title { get; set; }
    public string Content { get; set; }  // Markdown
    public string? TargetElement { get; set; }  // CSS selector
    public string? Action { get; set; }  // "click", "drag", "type"
    public string? ValidationScript { get; set; }  // JS para validar
}
```

**Tutoriais Planejados:**
1. "Sua Primeira Tela" - Básico
2. "Navegando entre Telas" - Básico
3. "Usando Assets" - Intermediário
4. "Gerando Pixel Art com IA" - Intermediário
5. "Exportando para Arduino" - Avançado

**Estimativa:** 3-4 semanas

---

### 11. Versionamento de Projetos

**Objetivo:** Histórico de alterações com possibilidade de restore.

**Entidades:**

```csharp
public class ProjectVersion
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public int VersionNumber { get; set; }
    public string ProjectJson { get; set; }
    public string? ChangeDescription { get; set; }
    public DateTime CreatedAt { get; set; }
    public int CreatedByUserId { get; set; }
    
    public UserProject? Project { get; set; }
}
```

**Funcionalidades:**
- Auto-save a cada 5 minutos com nova versão
- Limite de 50 versões por projeto
- Comparação visual entre versões
- Restore de versão anterior

**Estimativa:** 3 semanas

---

### 12. Simulador de Touch

**Objetivo:** Simular interações de toque no preview.

**Funcionalidades:**
- Clique em elementos com `TargetScreenId` navega para tela destino
- Simulação de swipe para navegação
- Indicador visual de área tocável
- Log de eventos de toque

**Implementação JavaScript:**

```javascript
class TouchSimulator {
    constructor(canvas, project) {
        this.canvas = canvas;
        this.project = project;
        this.setupEvents();
    }
    
    setupEvents() {
        this.canvas.addEventListener('click', (e) => {
            const rect = this.canvas.getBoundingClientRect();
            const x = e.clientX - rect.left;
            const y = e.clientY - rect.top;
            this.handleTouch(x, y);
        });
    }
    
    handleTouch(x, y) {
        const screen = this.getCurrentScreen();
        for (const el of screen.elements) {
            if (this.isPointInElement(x, y, el)) {
                if (el.targetScreenId) {
                    this.navigateTo(el.targetScreenId);
                    return;
                }
            }
        }
    }
}
```

**Estimativa:** 2 semanas

---

## ?? Especificações Técnicas

### Arquitetura de Dados para Projetos

```
???????????????????????????????????????????????????????????
?                    UserProjects                          ?
???????????????????????????????????????????????????????????
? Id (PK)                                                  ?
? UserId (FK ? Users)                                      ?
? Name                                                     ?
? Description                                              ?
? ProjectJson (LONGTEXT/JSON)                              ?
? ThumbnailBase64                                          ?
? IsPublic                                                 ?
? IsArchived                                               ?
? CreatedAt                                                ?
? UpdatedAt                                                ?
???????????????????????????????????????????????????????????
                     ?
                     ? 1:N
                     ?
???????????????????????????????????????????????????????????
?                   ProjectVersions                        ?
???????????????????????????????????????????????????????????
? Id (PK)                                                  ?
? ProjectId (FK ? UserProjects)                            ?
? VersionNumber                                            ?
? ProjectJson                                              ?
? ChangeDescription                                        ?
? CreatedAt                                                ?
? CreatedByUserId (FK ? Users)                             ?
???????????????????????????????????????????????????????????
```

### Migração de Dados

```csharp
public class MigrateToDatabase : IHostedService
{
    public async Task StartAsync(CancellationToken ct)
    {
        // 1. Verificar se existem projetos em arquivo
        var filePath = Path.Combine(_contentRoot, "project.json");
        if (File.Exists(filePath))
        {
            // 2. Ler projeto do arquivo
            var json = await File.ReadAllTextAsync(filePath, ct);
            
            // 3. Criar projeto no banco para o usuário admin
            var adminUser = await _context.Users.FirstOrDefaultAsync(u => u.Role == "Admin", ct);
            if (adminUser != null)
            {
                _context.UserProjects.Add(new UserProject
                {
                    UserId = adminUser.Id,
                    Name = "Projeto Migrado",
                    ProjectJson = json,
                    CreatedAt = DateTime.UtcNow
                });
                await _context.SaveChangesAsync(ct);
            }
            
            // 4. Mover arquivo para backup
            File.Move(filePath, filePath + ".bak");
        }
    }
}
```

---

## ?? Roadmap

### Q1 2025 (Janeiro - Março)

| Semana | Funcionalidade | Status |
|--------|----------------|--------|
| 1-2 | Correções de Segurança | ? Concluído |
| 3-5 | Persistência de Projetos | ?? Planejado |
| 6-8 | Biblioteca de Templates | ?? Planejado |
| 9-12 | SignalR - Sync em Tempo Real | ?? Planejado |

### Q2 2025 (Abril - Junho)

| Semana | Funcionalidade | Status |
|--------|----------------|--------|
| 1-4 | Preview em Dispositivo Real | ?? Planejado |
| 5-6 | Dashboard de Analytics | ?? Planejado |
| 7-10 | Exportação Multi-formato | ?? Planejado |
| 11-12 | Versionamento de Projetos | ?? Planejado |

### Q3 2025 (Julho - Setembro)

| Semana | Funcionalidade | Status |
|--------|----------------|--------|
| 1-4 | Integração PlatformIO | ?? Planejado |
| 5-8 | Sistema de Plugins | ?? Planejado |
| 9-12 | Tutoriais Interativos | ?? Planejado |

### Q4 2025 (Outubro - Dezembro)

| Semana | Funcionalidade | Status |
|--------|----------------|--------|
| 1-2 | Internacionalização | ?? Planejado |
| 3-4 | Simulador de Touch | ?? Planejado |
| 5-8 | Integração ESP-IDF | ?? Planejado |
| 9-12 | Refinamentos e Otimizações | ?? Planejado |

---

## ?? Métricas de Sucesso

### KPIs por Funcionalidade

| Funcionalidade | Métrica | Meta |
|----------------|---------|------|
| Persistência | Projetos salvos/dia | 100+ |
| SignalR | Sessões simultâneas | 50+ |
| Templates | Downloads de componentes | 500+/mês |
| Exportação | Exports/dia | 200+ |
| IA | Gerações de imagem/dia | 300+ |

### Qualidade

| Métrica | Meta |
|---------|------|
| Cobertura de Testes | > 80% |
| Bugs Críticos em Produção | 0 |
| Tempo de Resposta da API | < 200ms (p95) |
| Disponibilidade | > 99.5% |

---

*Documentação atualizada em Janeiro 2025*
*Versão 2.1*
