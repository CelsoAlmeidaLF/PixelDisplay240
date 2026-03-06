namespace Systekna.PixelDisplay.Application.Domain.Entities;

/// <summary>
/// Entidade raiz do projeto de prototipagem.
/// </summary>
public class PrototypeProject
{
    public List<PrototypeScreen> Screens { get; set; } = new();
    public string ActiveScreenId { get; set; } = "screen_1";
    public string? SelectedElementId { get; set; }
    public List<PrototypeAsset> Assets { get; set; } = new();
    public int ElementSeq { get; set; } = 1;
    public int ScreenSeq { get; set; } = 2;

    public PrototypeProject()
    {
        Screens.Add(new PrototypeScreen { Id = "screen_1", Name = "Home" });
    }
}
