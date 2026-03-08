namespace Systekna.PixelDisplay.Application.DTOs;

/// <summary>
/// DTO para requisições de adição de tela.
/// </summary>
public record AddScreenRequest(string? Name = null, string? Template = null);

/// <summary>
/// DTO para requisições de adição de elemento.
/// </summary>
public record AddElementRequest(string ScreenId, string Type, string? Asset = null);

/// <summary>
/// DTO para requisições de atualização de elemento.
/// </summary>
public record UpdateElementRequest(string ScreenId, string ElementId, Dictionary<string, object?> Patch);

/// <summary>
/// DTO para requisições de movimentação de elemento.
/// </summary>
public record MoveElementRequest(string ScreenId, string ElementId, int NewIndex);

/// <summary>
/// DTO para requisições de movimentação de tela.
/// </summary>
public record MoveScreenRequest(string ScreenId, int NewIndex);

/// <summary>
/// DTO para requisições de atualização de background.
/// </summary>
public record UpdateBackgroundRequest(string ScreenId, string? AssetName, string? DataUrl);

/// <summary>
/// DTO para requisições de atualização de tela.
/// </summary>
public record UpdateScreenRequest(string ScreenId, Dictionary<string, object?> Updates);
