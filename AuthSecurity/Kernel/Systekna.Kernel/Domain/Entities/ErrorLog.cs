using System.ComponentModel.DataAnnotations;

namespace Systekna.Kernel.Domain.Entities;

public class ErrorLog
{
    [Key]
    public int Id { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? StackTrace { get; set; }
    public string? Endpoint { get; set; }
    public string? Source { get; set; }
    public string? UserIdentifier { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string? IpAddress { get; set; }
}
