using System.ComponentModel.DataAnnotations;

namespace Systekna.Kernel.Domain.Entities;

public class AuditLog
{
    [Key]
    public int Id { get; set; }
    public string Action { get; set; } = string.Empty; // e.g., "Login", "UserDeleted", "RoleUpdated"
    public string UserIdentifier { get; set; } = string.Empty; // Username or ID
    public string Details { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string IpAddress { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
}
