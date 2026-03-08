using Systekna.PixelDisplay.Application.DTOs;

namespace Systekna.PixelDisplay.Application.Infrastructure.Interfaces;

/// <summary>
/// Interface para serviço de IA (geração de imagens e otimização de layout).
/// </summary>
public interface IAIService
{
    /// <summary>
    /// Gera uma imagem pixel art usando IA.
    /// </summary>
    Task<GenerateImageResult> GeneratePixelArtAsync(string apiKey, string prompt, int seed);
    
    /// <summary>
    /// Otimiza o layout dos elementos de uma tela usando IA.
    /// </summary>
    Task<OptimizeLayoutResult> OptimizeLayoutAsync(string apiKey, string elementsJson, string screenIntent);
}
