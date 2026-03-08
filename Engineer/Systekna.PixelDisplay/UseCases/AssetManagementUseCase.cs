using Systekna.PixelDisplay.Application.Domain.Aggregates;
using Systekna.PixelDisplay.Application.Domain.Entities;

namespace Systekna.PixelDisplay.Application.UseCases;

/// <summary>
/// Use Case para gerenciamento de assets do protótipo.
/// </summary>
public class AssetManagementUseCase
{
    private readonly MasterPrototype _master;

    public AssetManagementUseCase(MasterPrototype master) 
        => _master = master;

    /// <summary>
    /// Adiciona um novo asset ao projeto.
    /// </summary>
    public void AddAsset(PrototypeAsset asset) 
        => _master.AddAsset(asset);

    /// <summary>
    /// Remove um asset do projeto.
    /// </summary>
    public bool DeleteAsset(string name) 
        => _master.RemoveAsset(name);

    /// <summary>
    /// Obtém todos os assets do projeto.
    /// </summary>
    public IReadOnlyList<PrototypeAsset> GetAssets() 
        => _master.Project.Assets.AsReadOnly();

    /// <summary>
    /// Obtém um asset por nome.
    /// </summary>
    public PrototypeAsset? GetAsset(string name)
        => _master.Project.Assets.FirstOrDefault(a => a.Name == name);
}
