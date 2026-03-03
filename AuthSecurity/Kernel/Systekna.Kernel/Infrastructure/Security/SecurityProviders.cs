using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Systekna.Kernel.Domain.Interfaces;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Systekna.Kernel.Infrastructure.Security;

public class MyPasswordHasher : IPasswordHasher
{
    // BCrypt com work factor 12 (balanceado entre segurança e performance)
    private const int WorkFactor = 12;
    
    public string Hash(string password) => BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);
    public bool Verify(string password, string hash) => BCrypt.Net.BCrypt.Verify(password, hash);
}

public class MyJwtTokenProvider : ITokenProvider
{
    private readonly IConfiguration _config;
    private readonly byte[] _keyBytes;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly int _expirationHours;
    
    // Chave padrão para desenvolvimento (NUNCA usar em produção)
    private const string DefaultDevKey = "DEV_ONLY_KEY_DO_NOT_USE_IN_PRODUCTION_MIN_32_CHARS_REQUIRED";
    private const int MinKeyLength = 32;

    public MyJwtTokenProvider(IConfiguration config)
    {
        _config = config;
        var jwtSettings = config.GetSection("Jwt");
        
        var keyString = jwtSettings["Key"];
        
        // Validação da chave
        if (string.IsNullOrEmpty(keyString))
        {
            Console.WriteLine(">>> [SECURITY WARNING] JWT Key não configurada. Usando chave de desenvolvimento.");
            keyString = DefaultDevKey;
        }
        else if (keyString.Length < MinKeyLength)
        {
            Console.WriteLine($">>> [SECURITY WARNING] JWT Key muito curta ({keyString.Length} chars). Mínimo: {MinKeyLength} chars.");
        }
        
        _keyBytes = Encoding.UTF8.GetBytes(keyString);
        _issuer = jwtSettings["Issuer"] ?? "SysteknaAuth";
        _audience = jwtSettings["Audience"] ?? "SysteknaUsers";
        _expirationHours = int.TryParse(jwtSettings["ExpirationHours"], out var hours) ? hours : 2;
    }

    public string CreateToken(string userId, string username, string email, string role, string? policies = null)
    {
        var key = new SymmetricSecurityKey(_keyBytes);
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId),
            new Claim(JwtRegisteredClaimNames.UniqueName, username),
            new Claim(JwtRegisteredClaimNames.Email, email),
            new Claim(ClaimTypes.Role, role),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };

        // Adiciona policies como claims individuais para verificação granular
        if (!string.IsNullOrEmpty(policies))
        {
            foreach (var policy in policies.Split(';', StringSplitOptions.RemoveEmptyEntries))
            {
                claims.Add(new Claim("policy", policy.Trim()));
            }
        }

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddHours(_expirationHours),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        // Usa 64 bytes para maior segurança
        var randomNumber = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

    public ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
    {
        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = true,
            ValidAudience = _audience,
            ValidateIssuer = true,
            ValidIssuer = _issuer,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(_keyBytes),
            ValidateLifetime = false, // Permite tokens expirados para refresh
            ClockSkew = TimeSpan.Zero
        };

        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken securityToken);
            
            if (securityToken is not JwtSecurityToken jwtSecurityToken || 
                !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            {
                throw new SecurityTokenException("Algoritmo de token inválido");
            }

            return principal;
        }
        catch (Exception ex)
        {
            Console.WriteLine($">>> [JWT] Falha na validação do token: {ex.Message}");
            return null;
        }
    }
}
