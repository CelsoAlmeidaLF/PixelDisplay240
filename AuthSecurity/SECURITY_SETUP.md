# Configuração de Segredos para Desenvolvimento

## ?? IMPORTANTE: Nunca commite segredos no Git!

Este projeto usa **User Secrets** para armazenar dados sensíveis em desenvolvimento.

### Configuração Inicial (uma vez por máquina)

```bash
# No diretório do projeto WbAuth
cd AuthSecurity\Web\WbAuth
dotnet user-secrets init
dotnet user-secrets set "Jwt:Key" "SuaChaveJwtSuperSeguraComPeloMenos32Caracteres!@#$"
dotnet user-secrets set "ConnectionStrings:AuthConnection" "Server=localhost;Database=db_auth;User Id=app_auth;Password=SuaSenhaSecreta;"
dotnet user-secrets set "Smtp:Password" "SuaSenhaSmtp"
```

```bash
# No diretório do projeto WbAdmin
cd AuthSecurity\Web\WbAdmin
dotnet user-secrets init
dotnet user-secrets set "Jwt:Key" "SuaChaveJwtSuperSeguraComPeloMenos32Caracteres!@#$"
dotnet user-secrets set "ConnectionStrings:AuthConnection" "Server=localhost;Database=db_auth;User Id=app_auth;Password=SuaSenhaSecreta;"
```

### Verificar segredos configurados

```bash
dotnet user-secrets list
```

### Produção

Em produção, use:
- **Azure Key Vault** ou **AWS Secrets Manager**
- **Variáveis de ambiente** do container/servidor
- **Arquivo de configuração** fora do repositório Git

### Exemplo de configuração via variáveis de ambiente:

```bash
# Linux/macOS
export Jwt__Key="SuaChaveJwtSuperSeguraComPeloMenos32Caracteres"
export ConnectionStrings__AuthConnection="Server=...;Password=..."

# Windows PowerShell
$env:Jwt__Key="SuaChaveJwtSuperSeguraComPeloMenos32Caracteres"
$env:ConnectionStrings__AuthConnection="Server=...;Password=..."
```

## Requisitos de Senha JWT

A chave JWT deve ter:
- **Mínimo 32 caracteres** (256 bits para HS256)
- Recomendado: 64+ caracteres aleatórios
- Use um gerador seguro:

```csharp
// Gerar chave segura
var key = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(64));
Console.WriteLine(key);
```

## Checklist de Segurança

- [ ] Chave JWT com 32+ caracteres
- [ ] Senhas de banco não no appsettings.json
- [ ] Senhas SMTP não no appsettings.json
- [ ] .gitignore inclui arquivos de secrets
- [ ] Rate limiting habilitado em produção
- [ ] HTTPS obrigatório em produção
- [ ] Logs de auditoria ativos
