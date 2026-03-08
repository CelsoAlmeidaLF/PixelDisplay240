namespace Systekna.PixelDisplay.Application.Infrastructure.Interfaces;

/// <summary>
/// Interface para serviço de logs.
/// </summary>
public interface ILogRepository
{
    /// <summary>
    /// Salva um registro de erro.
    /// </summary>
    void SaveError(string context, string type, string message, string data);
}
