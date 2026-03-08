namespace Systekna.PixelDisplay.Application.Domain.Entities;

/// <summary>
/// Representa uma tela do protótipo.
/// </summary>
public class PrototypeScreen
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Background { get; set; }
    public string? BackgroundAsset { get; set; }
    public string? BackgroundColor { get; set; } = "#000000";
    public List<PrototypeElement> Elements { get; set; } = new();
}
