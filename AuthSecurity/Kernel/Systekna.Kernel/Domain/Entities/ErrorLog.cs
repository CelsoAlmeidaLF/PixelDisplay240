using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Systekna.Kernel.Domain.Entities;

public class ErrorLog
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [MaxLength(2000)]
    public string Message { get; set; } = string.Empty;

    public string? StackTrace { get; set; }

    [MaxLength(500)]
    public string? Endpoint { get; set; }

    [MaxLength(200)]
    public string? Source { get; set; }

    [MaxLength(200)]
    public string? UserIdentifier { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    [MaxLength(50)]
    public string? IpAddress { get; set; }

    /// <summary>
    /// Identificador único da requisição para correlação de logs
    /// </summary>
    [MaxLength(100)]
    public string? TraceId { get; set; }

    /// <summary>
    /// Severidade do erro (Information, Warning, Error, Critical)
    /// </summary>
    [MaxLength(20)]
    public string Severity { get; set; } = "Error";

    /// <summary>
    /// Categoria/módulo onde o erro ocorreu
    /// </summary>
    [MaxLength(100)]
    public string? Category { get; set; }
}
