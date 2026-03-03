namespace Systekna.Application.DTOs;

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
    public bool EnableAI { get; set; } = true;
    public bool EnableExport { get; set; } = true;
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
    public string GeminiModel { get; set; } = "gemini-2.5-flash-image";
    public string GeminiTextModel { get; set; } = "gemini-1.5-flash";
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

/// <summary>
/// Configurações de exportação de hardware
/// </summary>
public class HardwareExportOptions
{
    public const string SectionName = "HardwareExport";

    public string DefaultStorageType { get; set; } = "flash";
    public int MaxAssetSize { get; set; } = 57600; // 240x240 pixels
    public bool IncludeLittleFS { get; set; } = true;
}
