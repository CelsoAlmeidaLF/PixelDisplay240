namespace Systekna.PixelDisplay.Application.Domain.Entities;

/// <summary>
/// Representa um asset (imagem/sprite) do projeto.
/// </summary>
public class PrototypeAsset
{
    public string Name { get; set; } = string.Empty;
    public string DataUrl { get; set; } = string.Empty;
    public int Width { get; set; } = 240;
    public int Height { get; set; } = 240;
    public string Kind { get; set; } = "image";
    
    /// <summary>
    /// Tipo de armazenamento: "flash" (PROGMEM) ou "littlefs"
    /// </summary>
    public string StorageType { get; set; } = "flash";
}
