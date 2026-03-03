# PixelDisplay240 - IDE de Prototipagem para Displays TFT

[![.NET 8](https://img.shields.io/badge/.NET-8.0-purple.svg)](https://dotnet.microsoft.com/)
[![Razor Pages](https://img.shields.io/badge/ASP.NET-Razor%20Pages-blue.svg)](https://docs.microsoft.com/aspnet/core/razor-pages/)
[![License](https://img.shields.io/badge/license-MIT-green.svg)](LICENSE)

## ?? Descrição

**PixelDisplay240** é uma IDE web completa para desenvolvimento e prototipagem de interfaces para displays TFT 240x240 pixels. Permite criar telas interativas visualmente através de drag-and-drop e exportar código Arduino/ESP32 otimizado para a biblioteca TFT_eSPI.

## ? Funcionalidades

- ?? **Editor Visual**: Arrastar e soltar elementos no canvas 240x240
- ?? **Múltiplas Telas**: Criar e gerenciar várias telas do projeto
- ?? **Navegação**: Links entre telas para prototipagem de fluxos
- ??? **Assets**: Gerenciamento de imagens e sprites
- ?? **Bindings**: Vincular propriedades a estados dinâmicos
- ?? **IA Generativa**: Gerar pixel art com Google Gemini
- ?? **Auto-Layout**: Otimização de layout com IA
- ?? **Exportação**: Gerar projeto completo para Arduino IDE

## ?? Começando

### Pré-requisitos

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Visual Studio 2022 ou VS Code

### Executando o Projeto

```bash
# Clone o repositório
git clone https://github.com/CelsoAlmeidaLF/PixelDisplay240-PRO.git

# Entre no diretório
cd PixelDisplay240-Inside/Inside

# Restaurar dependências
dotnet restore

# Executar em modo desenvolvimento
dotnet run
```

O aplicativo estará disponível em `https://localhost:5001` ou `http://localhost:5000`.

## ?? Estrutura do Projeto

```
Inside/
??? Controllers/
?   ??? HomeController.cs          # Página principal (IDE)
?   ??? AccountController.cs       # Páginas de conta
??? Endpoints/
?   ??? AuthEndpoints.cs           # Autenticação de usuários
?   ??? ProfileEndpoints.cs        # Perfil e preferências
?   ??? GovernanceEndpoints.cs     # Governança (Admin)
?   ??? EndpointExtensions.cs      # Mapeamento centralizado
??? Views/
?   ??? Home/
?   ?   ??? Index.cshtml           # IDE principal
?   ?   ??? _DesignView.cshtml     # Painel de design
?   ?   ??? _PrototypeView.cshtml  # Preview do protótipo
?   ?   ??? _HardwareView.cshtml   # Config. de hardware
?   ??? Account/                   # Páginas de conta
?   ??? Shared/                    # Layouts compartilhados
??? wwwroot/
?   ??? css/                       # Estilos
?   ??? js/
?   ?   ??? model.js               # Modelo de dados
?   ?   ??? tft-commands.js        # Comandos TFT_eSPI
?   ?   ??? view.js                # Renderização canvas
?   ?   ??? controller.js          # Lógica de interação
?   ?   ??? designer.js            # Editor de elementos
?   ??? images/                    # Imagens estáticas
??? Program.cs                     # Configuração da aplicação
```

## ?? Autenticação

O sistema utiliza JWT Bearer para autenticação. Endpoints disponíveis:

| Método | Endpoint | Descrição |
|--------|----------|-----------|
| POST | `/api/auth/login` | Login com JWT |
| POST | `/api/auth/register` | Registro de usuário |
| POST | `/api/auth/refresh` | Renovar token |
| POST | `/api/auth/logout` | Logout |

## ?? API do Protótipo

| Método | Endpoint | Descrição |
|--------|----------|-----------|
| GET | `/api/prototype` | Obter projeto atual |
| POST | `/api/prototype/save` | Salvar projeto |
| GET | `/api/prototype/export` | Exportar para Arduino |
| GET | `/api/ai/image` | Gerar pixel art com IA |
| POST | `/api/ai/auto-layout` | Otimizar layout com IA |

## ?? Configuração

### appsettings.json

```json
{
  "Jwt": {
    "Key": "sua-chave-secreta",
    "Issuer": "PixelDisplay240",
    "Audience": "PixelDisplay240Clients"
  },
  "Features": {
    "EnableAI": true,
    "EnableExport": true
  },
  "AI": {
    "MaxPromptLength": 500,
    "TimeoutSeconds": 60
  }
}
```

### Chave da API Gemini

Configure a chave do Gemini através do endpoint `/api/config`:

```json
POST /api/config
{
  "Key": "GeminiKey",
  "Value": "sua-chave-gemini"
}
```

## ?? Elementos Suportados

| Tipo | Descrição | Comando TFT_eSPI |
|------|-----------|------------------|
| `fillRect` | Retângulo preenchido | `tft.fillRect()` |
| `drawRect` | Retângulo contornado | `tft.drawRect()` |
| `fillCircle` | Círculo preenchido | `tft.fillCircle()` |
| `drawCircle` | Círculo contornado | `tft.drawCircle()` |
| `fillTriangle` | Triângulo preenchido | `tft.fillTriangle()` |
| `drawLine` | Linha | `tft.drawLine()` |
| `drawString` | Texto | `tft.drawString()` |
| `pushImage` | Imagem/sprite | `tft.pushImage()` |

## ?? Políticas de Autorização

- `ClienteOuAdmin`: Acesso geral ao PixelDisplay
- `PixelDisplay.AI`: Acesso às funcionalidades de IA
- `PixelDisplay.Export`: Acesso à exportação de projetos
- `PixelDisplay.Projects`: Gerenciamento de projetos

## ?? Licença

Este projeto está licenciado sob a Licença MIT - veja o arquivo [LICENSE](LICENSE) para detalhes.

## ?? Contribuição

1. Fork o projeto
2. Crie sua Feature Branch (`git checkout -b feature/AmazingFeature`)
3. Commit suas mudanças (`git commit -m 'Add some AmazingFeature'`)
4. Push para a Branch (`git push origin feature/AmazingFeature`)
5. Abra um Pull Request

---

**Desenvolvido com ?? para a comunidade de desenvolvedores embarcados**
