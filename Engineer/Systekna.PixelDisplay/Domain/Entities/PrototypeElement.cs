namespace Systekna.PixelDisplay.Application.Domain.Entities;

/// <summary>
/// Representa um elemento UI dentro de uma tela.
/// </summary>
public class PrototypeElement
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = "rect";
    public string Name { get; set; } = string.Empty;
    public int X { get; set; } = 10;
    public int Y { get; set; } = 10;
    public int W { get; set; } = 50;
    public int H { get; set; } = 50;
    public string Color { get; set; } = "#38bdf8";
    public string? Asset { get; set; }
    public string? TargetScreenId { get; set; }

    // State Logic Bindings
    public string? XBind { get; set; }
    public string? YBind { get; set; }
    public string? WBind { get; set; }
    public string? HBind { get; set; }
    public string? ColorBind { get; set; }
    public string? ValueBind { get; set; }
}
