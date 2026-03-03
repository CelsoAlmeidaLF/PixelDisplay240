namespace PixelDisplay240Api.Models;

/// <summary>
/// Configurações de autenticação JWT
/// </summary>
public class AuthOptions
{
    public const string SectionName = "Auth";
    
    public string JwtKey { get; set; } = string.Empty;
    public string Issuer { get; set; } = "PixelDisplay240";
    public string Audience { get; set; } = "PixelDisplay240Clients";
    public int TokenExpirationHours { get; set; } = 4;
}

/// <summary>
/// Configurações de features toggles
/// </summary>
public class FeatureOptions
{
    public const string SectionName = "Features";
    
    public bool EnableLogs { get; set; } = true;
    public bool EnablePlaceholder { get; set; } = true;
}

/// <summary>
/// Configurações do serviço de IA
/// </summary>
public class AIOptions
{
    public const string SectionName = "AI";
    
    public string PlaceholderPath { get; set; } = "wwwroot/ai-placeholder.svg";
    public int MaxPromptLength { get; set; } = 500;
    public int TimeoutSeconds { get; set; } = 60;
}

/// <summary>
/// Configurações de rate limiting
/// </summary>
public class RateLimitingOptions
{
    public const string SectionName = "RateLimiting";
    
    public int PermitLimit { get; set; } = 100;
    public int WindowSeconds { get; set; } = 60;
    public int QueueLimit { get; set; } = 10;
}

/// <summary>
/// Configurações de CORS
/// </summary>
public class CorsOptions
{
    public const string SectionName = "Cors";
    
    public string[] AllowedOrigins { get; set; } = Array.Empty<string>();
}
