using Systekna.PixelDisplay.Application.Domain.Aggregates;
using Systekna.PixelDisplay.Application.Domain.Entities;

namespace Systekna.PixelDisplay.Application.UseCases;

/// <summary>
/// Use Case para gerenciamento de telas do protótipo.
/// </summary>
public class ScreenManagementUseCase
{
    private readonly MasterPrototype _master;

    public ScreenManagementUseCase(MasterPrototype master) 
        => _master = master;

    /// <summary>
    /// Adiciona uma nova tela ao projeto.
    /// </summary>
    public PrototypeScreen AddScreen(string? name = null, string? template = null)
    {
        var screen = _master.AddScreen(name);

        if (!string.IsNullOrEmpty(template))
        {
            ApplyTemplate(screen, template);
        }

        return screen;
    }

    /// <summary>
    /// Remove uma tela do projeto.
    /// </summary>
    public bool DeleteScreen(string id) 
        => _master.RemoveScreen(id);

    /// <summary>
    /// Seleciona uma tela como ativa.
    /// </summary>
    public void SelectScreen(string id)
    {
        if (_master.GetScreen(id) != null)
        {
            _master.Project.ActiveScreenId = id;
            _master.Project.SelectedElementId = null;
        }
    }

    /// <summary>
    /// Move uma tela para uma nova posição.
    /// </summary>
    public void MoveScreen(string screenId, int newIndex)
    {
        var screen = _master.GetScreen(screenId);
        if (screen == null) return;

        _master.Project.Screens.Remove(screen);
        int target = Math.Max(0, Math.Min(newIndex, _master.Project.Screens.Count));
        _master.Project.Screens.Insert(target, screen);
    }

    /// <summary>
    /// Atualiza o background de uma tela.
    /// </summary>
    public void UpdateScreenBackground(string screenId, string? assetName, string? dataUrl)
    {
        var screen = _master.GetScreen(screenId);
        if (screen == null) return;
        screen.BackgroundAsset = assetName;
        screen.Background = dataUrl;
    }

    /// <summary>
    /// Atualiza propriedades de uma tela.
    /// </summary>
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
    }

    private void ApplyTemplate(PrototypeScreen screen, string template)
    {
        switch (template.ToLower())
        {
            case "loading":
                _master.AddElement(screen.Id, "fillScreen", null)!.Color = "#000000";
                _master.AddElement(screen.Id, "drawCentreString", null)!.Name = "Carregando...";
                _master.AddElement(screen.Id, "drawRect", null)!.Name = "ProgressBarBorder";
                break;
            case "menu":
                _master.AddElement(screen.Id, "fillRect", null)!.Name = "HeaderBg";
                _master.AddScreen("SubMenu");
                break;
            case "dashboard":
                var header = _master.AddElement(screen.Id, "fillRect", null)!;
                header.Name = "Header";
                header.X = 0; header.Y = 0; header.W = 240; header.H = 30;
                header.Color = "#1e293b";

                var cpuText = _master.AddElement(screen.Id, "drawCentreString", null)!;
                cpuText.Name = "CPU: 45%";
                cpuText.X = 120; cpuText.Y = 8;
                cpuText.Color = "#38bdf8";

                var statusCircle = _master.AddElement(screen.Id, "fillCircle", null)!;
                statusCircle.Name = "Status_OK";
                statusCircle.X = 190; statusCircle.Y = 15; statusCircle.W = 20; statusCircle.H = 20;
                statusCircle.Color = "#4ade80";
                break;
            case "clock":
                var clockFace = _master.AddElement(screen.Id, "drawCircle", null)!;
                clockFace.Name = "ClockFace";
                clockFace.X = 20; clockFace.Y = 20; clockFace.W = 200; clockFace.H = 200;
                clockFace.Color = "#1e293b";

                var timeText = _master.AddElement(screen.Id, "drawCentreString", null)!;
                timeText.Name = "12:45";
                timeText.X = 120; timeText.Y = 90;
                timeText.Color = "#ffffff";
                break;
        }
    }
}
