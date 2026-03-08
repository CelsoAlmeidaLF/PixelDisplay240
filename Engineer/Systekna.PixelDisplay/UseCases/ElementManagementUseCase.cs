using Systekna.PixelDisplay.Application.Domain.Aggregates;
using Systekna.PixelDisplay.Application.Domain.Entities;

namespace Systekna.PixelDisplay.Application.UseCases;

/// <summary>
/// Use Case para gerenciamento de elementos do protótipo.
/// </summary>
public class ElementManagementUseCase
{
    private readonly MasterPrototype _master;

    public ElementManagementUseCase(MasterPrototype master) 
        => _master = master;

    /// <summary>
    /// Adiciona um novo elemento a uma tela.
    /// </summary>
    public PrototypeElement? AddElement(string screenId, string type, string? asset = null) 
        => _master.AddElement(screenId, type, asset);

    /// <summary>
    /// Remove um elemento de uma tela.
    /// </summary>
    public bool DeleteElement(string elId) 
        => _master.RemoveElement(elId);

    /// <summary>
    /// Atualiza propriedades de um elemento.
    /// </summary>
    public void PatchElement(string elId, Dictionary<string, object?> patch)
    {
        var el = _master.FindElement(elId);
        if (el == null) return;

        foreach (var kv in patch)
        {
            var val = kv.Value?.ToString();
            switch (kv.Key.ToLower())
            {
                case "name": if (!string.IsNullOrEmpty(val)) el.Name = val; break;
                case "color": if (!string.IsNullOrEmpty(val)) el.Color = val; break;
                case "asset": el.Asset = val; break;
                case "targetscreenid": el.TargetScreenId = val; break;
                case "x": if (int.TryParse(val, out var x)) el.X = x; break;
                case "y": if (int.TryParse(val, out var y)) el.Y = y; break;
                case "w": if (int.TryParse(val, out var w) && w > 0) el.W = w; break;
                case "h": if (int.TryParse(val, out var h) && h > 0) el.H = h; break;
                case "xbind": el.XBind = val; break;
                case "ybind": el.YBind = val; break;
                case "wbind": el.WBind = val; break;
                case "hbind": el.HBind = val; break;
                case "colorbind": el.ColorBind = val; break;
                case "valuebind": el.ValueBind = val; break;
            }
        }
    }

    /// <summary>
    /// Move um elemento para uma nova posição dentro da mesma tela.
    /// </summary>
    public void MoveElement(string screenId, string elId, int newIndex)
    {
        var screen = _master.GetScreen(screenId);
        var el = _master.FindElement(elId);
        if (screen == null || el == null) return;

        screen.Elements.Remove(el);
        int target = Math.Max(0, Math.Min(newIndex, screen.Elements.Count));
        screen.Elements.Insert(target, el);
    }

    /// <summary>
    /// Move um elemento para outra tela.
    /// </summary>
    public void MoveElementToScreen(string elId, string targetScreenId) 
        => _master.MoveElementToScreen(elId, targetScreenId);

    /// <summary>
    /// Seleciona um elemento.
    /// </summary>
    public void SelectElement(string? elId) 
        => _master.Project.SelectedElementId = elId;
}
