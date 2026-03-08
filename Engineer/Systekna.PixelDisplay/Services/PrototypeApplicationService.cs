using Systekna.PixelDisplay.Application.Domain.Aggregates;
using Systekna.PixelDisplay.Application.Domain.Entities;
using Systekna.PixelDisplay.Application.UseCases;

namespace Systekna.PixelDisplay.Application.Services;

/// <summary>
/// Serviço de aplicação para gerenciamento do protótipo.
/// Orquestra os Use Cases e mantém o estado do projeto.
/// </summary>
public class PrototypeApplicationService
{
    private MasterPrototype _master;
    private readonly ScreenManagementUseCase _screenUseCase;
    private readonly ElementManagementUseCase _elementUseCase;
    private readonly AssetManagementUseCase _assetUseCase;

    public PrototypeApplicationService()
    {
        _master = new MasterPrototype(new PrototypeProject());
        _screenUseCase = new ScreenManagementUseCase(_master);
        _elementUseCase = new ElementManagementUseCase(_master);
        _assetUseCase = new AssetManagementUseCase(_master);
    }

    /// <summary>
    /// Obtém o projeto atual.
    /// </summary>
    public PrototypeProject GetProject() => _master.Project;

    #region Screen Operations

    public PrototypeScreen AddScreen(string? name = null, string? template = null)
        => _screenUseCase.AddScreen(name, template);

    public bool DeleteScreen(string id)
        => _screenUseCase.DeleteScreen(id);

    public void SelectScreen(string id)
        => _screenUseCase.SelectScreen(id);

    public void MoveScreen(string screenId, int newIndex)
        => _screenUseCase.MoveScreen(screenId, newIndex);

    public void UpdateScreenBackground(string screenId, string? assetName, string? dataUrl)
        => _screenUseCase.UpdateScreenBackground(screenId, assetName, dataUrl);

    public void UpdateScreen(string screenId, Dictionary<string, object?> updates)
        => _screenUseCase.UpdateScreen(screenId, updates);

    #endregion

    #region Element Operations

    public PrototypeElement? AddElement(string screenId, string type, string? asset = null)
        => _elementUseCase.AddElement(screenId, type, asset);

    public bool DeleteElement(string elId)
        => _elementUseCase.DeleteElement(elId);

    public void PatchElement(string elId, Dictionary<string, object?> patch)
        => _elementUseCase.PatchElement(elId, patch);

    public void MoveElement(string screenId, string elId, int newIndex)
        => _elementUseCase.MoveElement(screenId, elId, newIndex);

    public void SelectElement(string? elId)
        => _elementUseCase.SelectElement(elId);

    #endregion

    #region Asset Operations

    public void AddAsset(PrototypeAsset asset)
        => _assetUseCase.AddAsset(asset);

    public bool DeleteAsset(string name)
        => _assetUseCase.DeleteAsset(name);

    #endregion

    #region Project Operations

    /// <summary>
    /// Salva/carrega um projeto completo.
    /// </summary>
    public void SaveProject(PrototypeProject newProject)
    {
        if (newProject == null) return;
        _master = new MasterPrototype(newProject);
        
        // Recria os use cases com o novo master
        Console.WriteLine($"[PrototypeApplicationService] Projeto recarregado. Telas: {newProject.Screens.Count}");
    }

    #endregion
}
