using Microsoft.Extensions.Logging;
using Systekna.Application.Domain.Aggregates;
using Systekna.Application.Domain.Entities;

namespace Systekna.Application.Services;

/// <summary>
/// Interface para o serviço de prototipagem de interfaces.
/// </summary>
public interface IPrototypeService
{
    PrototypeProject GetProject();
    PrototypeScreen AddScreen(string? name = null, string? template = null);
    bool DeleteScreen(string id);
    void SelectScreen(string id);
    void MoveScreen(string screenId, int newIndex);
    PrototypeElement? AddElement(string screenId, string type, string? asset = null);
    bool DeleteElement(string screenId, string elId);
    void PatchElement(string screenId, string elId, Dictionary<string, object?> patch);
    void MoveElement(string screenId, string elId, int newIndex);
    void AddAsset(PrototypeAsset asset);
    void DeleteAsset(string name);
    void UpdateScreenBackground(string screenId, string? assetName, string? dataUrl);
    void UpdateScreen(string screenId, Dictionary<string, object?> updates);
    void SaveProject(PrototypeProject newProject);
}

/// <summary>
/// Serviço de prototipagem de interfaces para displays TFT.
/// </summary>
public class PrototypeService : IPrototypeService
{
    private MasterPrototype _master;
    private readonly ILogger<PrototypeService> _logger;

    public PrototypeService(ILogger<PrototypeService> logger)
    {
        _logger = logger;
        _master = new MasterPrototype(new PrototypeProject());
        _logger.LogDebug("PrototypeService inicializado com projeto vazio");
    }

    public PrototypeProject GetProject() => _master.Project;

    public PrototypeScreen AddScreen(string? name = null, string? template = null)
    {
        var screen = _master.AddScreen(name);
        _logger.LogDebug("Tela '{ScreenName}' adicionada com ID {ScreenId}", screen.Name, screen.Id);

        // Template Logic
        if (!string.IsNullOrEmpty(template))
        {
            ApplyTemplate(screen, template);
            _logger.LogDebug("Template '{Template}' aplicado à tela {ScreenId}", template, screen.Id);
        }

        return screen;
    }

    private void ApplyTemplate(PrototypeScreen screen, string template)
    {
        switch (template.ToLower())
        {
            case "loading":
                var loadEl = _master.AddElement(screen.Id, "fillScreen", null);
                if (loadEl != null) loadEl.Color = "#000000";
                var textEl = _master.AddElement(screen.Id, "drawCentreString", null);
                if (textEl != null) textEl.Name = "Carregando...";
                var borderEl = _master.AddElement(screen.Id, "drawRect", null);
                if (borderEl != null) borderEl.Name = "ProgressBarBorder";
                break;
            case "menu":
                var headerEl = _master.AddElement(screen.Id, "fillRect", null);
                if (headerEl != null) headerEl.Name = "HeaderBg";
                _master.AddScreen("SubMenu");
                break;
            case "dashboard":
                _master.AddElement(screen.Id, "fillRect", null);
                _master.AddElement(screen.Id, "drawCentreString", null);
                _master.AddElement(screen.Id, "fillCircle", null);
                break;
        }
    }

    public bool DeleteScreen(string id)
    {
        var result = _master.RemoveScreen(id);
        if (result)
        {
            _logger.LogDebug("Tela {ScreenId} removida", id);
        }
        return result;
    }

    public void SelectScreen(string id)
    {
        if (_master.GetScreen(id) != null)
        {
            _master.Project.ActiveScreenId = id;
            _master.Project.SelectedElementId = null;
            _logger.LogDebug("Tela {ScreenId} selecionada", id);
        }
    }

    public void MoveScreen(string screenId, int newIndex)
    {
        var screen = _master.GetScreen(screenId);
        if (screen == null) return;

        _master.Project.Screens.Remove(screen);
        int target = Math.Max(0, Math.Min(newIndex, _master.Project.Screens.Count));
        _master.Project.Screens.Insert(target, screen);
        _logger.LogDebug("Tela {ScreenId} movida para posição {Index}", screenId, target);
    }

    public PrototypeElement? AddElement(string screenId, string type, string? asset = null)
    {
        var element = _master.AddElement(screenId, type, asset);
        if (element != null)
        {
            _logger.LogDebug("Elemento '{ElementType}' adicionado à tela {ScreenId}", type, screenId);
        }
        return element;
    }

    public bool DeleteElement(string screenId, string elId)
    {
        var result = _master.RemoveElement(elId);
        if (result)
        {
            _logger.LogDebug("Elemento {ElementId} removido da tela {ScreenId}", elId, screenId);
        }
        return result;
    }

    public void PatchElement(string screenId, string elId, Dictionary<string, object?> patch)
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
        _logger.LogDebug("Elemento {ElementId} atualizado com {PatchCount} propriedades", elId, patch.Count);
    }

    public void MoveElement(string screenId, string elId, int newIndex)
    {
        var screen = _master.GetScreen(screenId);
        var el = _master.FindElement(elId);
        if (screen == null || el == null) return;

        screen.Elements.Remove(el);
        int target = Math.Max(0, Math.Min(newIndex, screen.Elements.Count));
        screen.Elements.Insert(target, el);
        _logger.LogDebug("Elemento {ElementId} movido para posição {Index}", elId, target);
    }

    public void AddAsset(PrototypeAsset asset)
    {
        _master.AddAsset(asset);
        _logger.LogDebug("Asset '{AssetName}' adicionado ({Width}x{Height})", asset.Name, asset.Width, asset.Height);
    }

    public void DeleteAsset(string name)
    {
        var asset = _master.Project.Assets.FirstOrDefault(a => a.Name == name);
        if (asset == null) return;

        _master.Project.Assets.Remove(asset);
        foreach (var s in _master.Project.Screens)
        {
            if (s.BackgroundAsset == name) { s.BackgroundAsset = null; s.Background = null; }
            foreach (var el in s.Elements) { if (el.Asset == name) el.Asset = null; }
        }
        _logger.LogDebug("Asset '{AssetName}' removido e referências limpas", name);
    }

    public void UpdateScreenBackground(string screenId, string? assetName, string? dataUrl)
    {
        var screen = _master.GetScreen(screenId);
        if (screen == null) return;
        screen.BackgroundAsset = assetName;
        screen.Background = dataUrl;
        _logger.LogDebug("Background da tela {ScreenId} atualizado", screenId);
    }

    public void UpdateScreen(string screenId, Dictionary<string, object?> updates)
    {
        var screen = _master.GetScreen(screenId);
        if (screen == null) return;

        foreach (var kv in updates)
        {
            var val = kv.Value?.ToString();
            switch (kv.Key.ToLower())
            {
                case "name": if (!string.IsNullOrEmpty(val)) screen.Name = val; break;
                case "backgroundcolor": screen.BackgroundColor = val; break;
            }
        }
        _logger.LogDebug("Tela {ScreenId} atualizada com {UpdateCount} propriedades", screenId, updates.Count);
    }

    public void SaveProject(PrototypeProject newProject)
    {
        if (newProject == null) return;
        _master = new MasterPrototype(newProject);
        _logger.LogInformation("Projeto recarregado com {ScreenCount} telas e {AssetCount} assets", 
            newProject.Screens.Count, newProject.Assets.Count);
    }
}
