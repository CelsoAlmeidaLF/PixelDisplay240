namespace Systekna.PixelDisplay.Application.DTOs;

/// <summary>
/// DTO para requisições de geração de imagem via IA.
/// </summary>
public record GenerateImageRequest(string Prompt, int Seed = 0);

/// <summary>
/// DTO para resultado de geração de imagem.
/// </summary>
public record GenerateImageResult(bool Success, byte[]? Data, string? Error);

/// <summary>
/// DTO para requisições de otimização de layout via IA.
/// </summary>
public record OptimizeLayoutRequest(string ElementsJson, string ScreenIntent);

/// <summary>
/// DTO para resultado de otimização de layout.
/// </summary>
public record OptimizeLayoutResult(bool Success, string? ElementsJson, string? Error);
