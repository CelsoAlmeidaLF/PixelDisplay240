using Microsoft.EntityFrameworkCore;
using Systekna.Kernel.Domain.Entities;

namespace Systekna.Kernel.Infrastructure.Data;

public class AuthDbContext : DbContext
{
    public AuthDbContext(DbContextOptions<AuthDbContext> options) : base(options) { }

    public DbSet<UserEntity> Users { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }
    public DbSet<SystemSetting> SystemSettings { get; set; }
    public DbSet<ErrorLog> ErrorLogs { get; set; }
    public DbSet<RegisteredSystem> RegisteredSystems { get; set; }
    public DbSet<UserSystemAccess> UserSystemAccesses { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.Entity<UserEntity>(entity =>
        {
            entity.HasIndex(u => u.Username).IsUnique();
            entity.HasIndex(u => u.Email).IsUnique();
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasIndex(a => a.Timestamp);
        });

        // Configuração de RegisteredSystem
        modelBuilder.Entity<RegisteredSystem>(entity =>
        {
            entity.HasIndex(s => s.SystemCode).IsUnique();
            entity.HasIndex(s => s.IsActive);
        });

        // Configuração de UserSystemAccess (relação muitos-para-muitos)
        modelBuilder.Entity<UserSystemAccess>(entity =>
        {
            // Índice único para evitar duplicatas de usuário-sistema
            entity.HasIndex(usa => new { usa.UserId, usa.SystemId }).IsUnique();
            
            // Índices para consultas frequentes
            entity.HasIndex(usa => usa.UserId);
            entity.HasIndex(usa => usa.SystemId);
            entity.HasIndex(usa => usa.IsApproved);
            entity.HasIndex(usa => usa.IsActive);

            // Relacionamento com User
            entity.HasOne(usa => usa.User)
                .WithMany()
                .HasForeignKey(usa => usa.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Relacionamento com System
            entity.HasOne(usa => usa.System)
                .WithMany(s => s.UserAccesses)
                .HasForeignKey(usa => usa.SystemId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
