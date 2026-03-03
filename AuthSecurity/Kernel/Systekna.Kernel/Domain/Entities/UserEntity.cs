using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Systekna.Kernel.Domain.Entities;

public class UserEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string Username { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(255)]
    public string Email { get; set; } = string.Empty;
    
    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Role { get; set; } = "User";

    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public bool IsEmailVerified { get; set; } = false;
    public string? EmailConfirmationToken { get; set; }
    
    public string? PasswordResetToken { get; set; }
    public DateTime? ResetTokenExpires { get; set; }

    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiryTime { get; set; }

    public bool IsApproved { get; set; } = false;
    public bool IsActive { get; set; } = true;
    public string Policies { get; set; } = "BaseAccess"; // E.g. "SystemA;SystemB"
}
