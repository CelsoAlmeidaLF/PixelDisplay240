using System.Collections.Concurrent;

namespace Systekna.Kernel.Application.Services;

/// <summary>
/// Serviço de Rate Limiting para proteção contra ataques de força bruta
/// </summary>
public interface ILoginRateLimiter
{
    bool IsBlocked(string identifier);
    void RecordFailedAttempt(string identifier);
    void RecordSuccessfulLogin(string identifier);
    LoginAttemptInfo GetAttemptInfo(string identifier);
}

public class LoginRateLimiter : ILoginRateLimiter
{
    private readonly ConcurrentDictionary<string, LoginAttemptInfo> _attempts = new();
    private readonly RateLimitPolicy _policy;
    private readonly Timer _cleanupTimer;

    public LoginRateLimiter(RateLimitPolicy? policy = null)
    {
        _policy = policy ?? RateLimitPolicy.Default;
        
        // Limpeza periódica de entradas expiradas (a cada 5 minutos)
        _cleanupTimer = new Timer(CleanupExpiredEntries, null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
    }

    public bool IsBlocked(string identifier)
    {
        if (string.IsNullOrEmpty(identifier)) return false;
        
        if (!_attempts.TryGetValue(identifier.ToLowerInvariant(), out var info))
            return false;

        // Se o bloqueio expirou, libera
        if (info.BlockedUntil.HasValue && info.BlockedUntil.Value <= DateTime.UtcNow)
        {
            _attempts.TryRemove(identifier.ToLowerInvariant(), out _);
            return false;
        }

        return info.BlockedUntil.HasValue && info.BlockedUntil.Value > DateTime.UtcNow;
    }

    public void RecordFailedAttempt(string identifier)
    {
        if (string.IsNullOrEmpty(identifier)) return;

        var key = identifier.ToLowerInvariant();
        var now = DateTime.UtcNow;

        _attempts.AddOrUpdate(key,
            // Adiciona nova entrada
            _ => new LoginAttemptInfo
            {
                Identifier = key,
                FailedAttempts = 1,
                FirstAttempt = now,
                LastAttempt = now,
                BlockedUntil = null
            },
            // Atualiza entrada existente
            (_, existing) =>
            {
                // Reset se a janela expirou
                if (now - existing.FirstAttempt > _policy.WindowDuration)
                {
                    return new LoginAttemptInfo
                    {
                        Identifier = key,
                        FailedAttempts = 1,
                        FirstAttempt = now,
                        LastAttempt = now,
                        BlockedUntil = null
                    };
                }

                existing.FailedAttempts++;
                existing.LastAttempt = now;

                // Bloqueia se excedeu tentativas
                if (existing.FailedAttempts >= _policy.MaxAttempts)
                {
                    // Bloqueio progressivo
                    var blockDuration = CalculateBlockDuration(existing.FailedAttempts);
                    existing.BlockedUntil = now.Add(blockDuration);
                }

                return existing;
            });
    }

    public void RecordSuccessfulLogin(string identifier)
    {
        if (string.IsNullOrEmpty(identifier)) return;
        _attempts.TryRemove(identifier.ToLowerInvariant(), out _);
    }

    public LoginAttemptInfo GetAttemptInfo(string identifier)
    {
        if (string.IsNullOrEmpty(identifier))
            return new LoginAttemptInfo();

        return _attempts.TryGetValue(identifier.ToLowerInvariant(), out var info)
            ? info
            : new LoginAttemptInfo();
    }

    private TimeSpan CalculateBlockDuration(int failedAttempts)
    {
        // Bloqueio progressivo: 1min, 5min, 15min, 30min, 1h, 2h...
        var multiplier = Math.Min(failedAttempts - _policy.MaxAttempts + 1, 10);
        var baseSeconds = _policy.BaseLockoutDuration.TotalSeconds;
        return TimeSpan.FromSeconds(baseSeconds * Math.Pow(2, multiplier - 1));
    }

    private void CleanupExpiredEntries(object? state)
    {
        var now = DateTime.UtcNow;
        var expiredKeys = _attempts
            .Where(kvp => 
                (kvp.Value.BlockedUntil.HasValue && kvp.Value.BlockedUntil.Value < now) ||
                (now - kvp.Value.LastAttempt > TimeSpan.FromHours(24)))
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in expiredKeys)
        {
            _attempts.TryRemove(key, out _);
        }
    }
}

public class LoginAttemptInfo
{
    public string Identifier { get; set; } = string.Empty;
    public int FailedAttempts { get; set; }
    public DateTime FirstAttempt { get; set; }
    public DateTime LastAttempt { get; set; }
    public DateTime? BlockedUntil { get; set; }
    
    public bool IsBlocked => BlockedUntil.HasValue && BlockedUntil.Value > DateTime.UtcNow;
    public TimeSpan? TimeUntilUnblock => IsBlocked ? BlockedUntil!.Value - DateTime.UtcNow : null;
}

public class RateLimitPolicy
{
    /// <summary>
    /// Número máximo de tentativas antes do bloqueio
    /// </summary>
    public int MaxAttempts { get; set; } = 5;
    
    /// <summary>
    /// Janela de tempo para contar tentativas
    /// </summary>
    public TimeSpan WindowDuration { get; set; } = TimeSpan.FromMinutes(15);
    
    /// <summary>
    /// Duração base do bloqueio (aumenta progressivamente)
    /// </summary>
    public TimeSpan BaseLockoutDuration { get; set; } = TimeSpan.FromMinutes(1);

    public static RateLimitPolicy Default => new();

    /// <summary>
    /// Política estrita para produção
    /// </summary>
    public static RateLimitPolicy Strict => new()
    {
        MaxAttempts = 3,
        WindowDuration = TimeSpan.FromMinutes(30),
        BaseLockoutDuration = TimeSpan.FromMinutes(5)
    };

    /// <summary>
    /// Política relaxada para desenvolvimento
    /// </summary>
    public static RateLimitPolicy Development => new()
    {
        MaxAttempts = 100,
        WindowDuration = TimeSpan.FromMinutes(1),
        BaseLockoutDuration = TimeSpan.FromSeconds(10)
    };
}
