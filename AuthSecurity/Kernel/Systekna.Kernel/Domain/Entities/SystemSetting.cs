using System.ComponentModel.DataAnnotations;

namespace Systekna.Kernel.Domain.Entities;

public class SystemSetting
{
    [Key]
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}
