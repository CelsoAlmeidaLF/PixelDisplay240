using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Systekna.Kernel.Domain.Entities;

/// <summary>
/// Representa um sistema/aplicação cadastrado na plataforma de governança.
/// Cada sistema pode ter múltiplos usuários vinculados com diferentes níveis de acesso.
/// </summary>
public class RegisteredSystem
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>
    /// Identificador único do sistema (slug). Ex: "pixeldisplay240", "wbadmin"
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string SystemCode { get; set; } = string.Empty;

    /// <summary>
    /// Nome amigável do sistema. Ex: "PixelDisplay240 PRO", "WB Admin"
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Descrição do sistema
    /// </summary>
    [MaxLength(1000)]
    public string? Description { get; set; }

    /// <summary>
    /// URL base do sistema (para redirecionamentos e links)
    /// </summary>
    [MaxLength(500)]
    public string? BaseUrl { get; set; }

    /// <summary>
    /// URL do ícone/logo do sistema
    /// </summary>
    [MaxLength(500)]
    public string? IconUrl { get; set; }

    /// <summary>
    /// Políticas disponíveis neste sistema, separadas por ponto-e-vírgula.
    /// Ex: "PixelDisplay.AI;PixelDisplay.Export;PixelDisplay.Projects"
    /// </summary>
    [MaxLength(2000)]
    public string AvailablePolicies { get; set; } = string.Empty;

    /// <summary>
    /// Se o sistema está ativo e aceitando usuários
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Se novos usuários precisam de aprovação para acessar este sistema
    /// </summary>
    public bool RequiresApproval { get; set; } = true;

    /// <summary>
    /// Chave secreta para validação de comunicação entre sistemas (API Key)
    /// </summary>
    [MaxLength(500)]
    public string? ApiSecret { get; set; }

    /// <summary>
    /// Data de cadastro do sistema
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Última atualização do cadastro
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Navegação: Acessos de usuários a este sistema
    /// </summary>
    public virtual ICollection<UserSystemAccess> UserAccesses { get; set; } = new List<UserSystemAccess>();
}

/// <summary>
/// Representa o vínculo entre um usuário e um sistema cadastrado.
/// Permite controle granular de acesso por sistema.
/// </summary>
public class UserSystemAccess
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>
    /// ID do usuário
    /// </summary>
    [Required]
    public int UserId { get; set; }

    /// <summary>
    /// ID do sistema
    /// </summary>
    [Required]
    public int SystemId { get; set; }

    /// <summary>
    /// Role do usuário NESTE sistema específico. Ex: "Admin", "User", "Viewer"
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string SystemRole { get; set; } = "User";

    /// <summary>
    /// Políticas específicas do usuário neste sistema, separadas por ponto-e-vírgula.
    /// Se vazio, herda as políticas padrão do role.
    /// </summary>
    [MaxLength(2000)]
    public string GrantedPolicies { get; set; } = string.Empty;

    /// <summary>
    /// Se o acesso está ativo
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Se o acesso foi aprovado (quando o sistema requer aprovação)
    /// </summary>
    public bool IsApproved { get; set; } = false;

    /// <summary>
    /// Data da solicitação de acesso
    /// </summary>
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Data da aprovação (se aplicável)
    /// </summary>
    public DateTime? ApprovedAt { get; set; }

    /// <summary>
    /// ID do admin que aprovou o acesso
    /// </summary>
    public int? ApprovedByUserId { get; set; }

    /// <summary>
    /// Último acesso do usuário a este sistema
    /// </summary>
    public DateTime? LastAccessAt { get; set; }

    /// <summary>
    /// Notas administrativas sobre este acesso
    /// </summary>
    [MaxLength(1000)]
    public string? AdminNotes { get; set; }

    // Navegação
    [ForeignKey(nameof(UserId))]
    public virtual UserEntity? User { get; set; }

    [ForeignKey(nameof(SystemId))]
    public virtual RegisteredSystem? System { get; set; }
}
