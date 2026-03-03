using System.Text.RegularExpressions;

namespace Systekna.Kernel.Application.Services;

/// <summary>
/// Serviço para validação de força de senhas
/// </summary>
public interface IPasswordValidationService
{
    PasswordValidationResult Validate(string password);
}

public class PasswordValidationService : IPasswordValidationService
{
    private readonly PasswordPolicy _policy;

    public PasswordValidationService(PasswordPolicy? policy = null)
    {
        _policy = policy ?? PasswordPolicy.Default;
    }

    public PasswordValidationResult Validate(string password)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(password))
        {
            errors.Add("A senha não pode estar vazia.");
            return new PasswordValidationResult(false, errors);
        }

        if (password.Length < _policy.MinLength)
            errors.Add($"A senha deve ter pelo menos {_policy.MinLength} caracteres.");

        if (password.Length > _policy.MaxLength)
            errors.Add($"A senha não pode ter mais de {_policy.MaxLength} caracteres.");

        if (_policy.RequireUppercase && !password.Any(char.IsUpper))
            errors.Add("A senha deve conter pelo menos uma letra maiúscula.");

        if (_policy.RequireLowercase && !password.Any(char.IsLower))
            errors.Add("A senha deve conter pelo menos uma letra minúscula.");

        if (_policy.RequireDigit && !password.Any(char.IsDigit))
            errors.Add("A senha deve conter pelo menos um número.");

        if (_policy.RequireSpecialChar && !Regex.IsMatch(password, @"[!@#$%^&*(),.?""':{}|<>_\-+=\[\]\\\/`~]"))
            errors.Add("A senha deve conter pelo menos um caractere especial (!@#$%^&*...).");

        // Verificar senhas comuns (básico)
        if (_policy.RejectCommonPasswords && IsCommonPassword(password))
            errors.Add("Esta senha é muito comum. Escolha uma senha mais forte.");

        // Verificar sequências repetidas
        if (_policy.RejectRepeatedChars && HasRepeatedChars(password, 3))
            errors.Add("A senha não pode ter 3 ou mais caracteres repetidos consecutivamente.");

        return new PasswordValidationResult(errors.Count == 0, errors);
    }

    private static bool IsCommonPassword(string password)
    {
        var commonPasswords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "password", "123456", "12345678", "qwerty", "abc123", "monkey", "1234567",
            "letmein", "trustno1", "dragon", "baseball", "iloveyou", "master", "sunshine",
            "ashley", "bailey", "shadow", "123123", "654321", "superman", "qazwsx",
            "michael", "football", "password1", "password123", "admin", "admin123",
            "root", "toor", "pass", "test", "guest", "master", "changeme", "welcome"
        };
        return commonPasswords.Contains(password);
    }

    private static bool HasRepeatedChars(string password, int maxRepeat)
    {
        for (int i = 0; i <= password.Length - maxRepeat; i++)
        {
            if (password.Substring(i, maxRepeat).Distinct().Count() == 1)
                return true;
        }
        return false;
    }
}

public record PasswordValidationResult(bool IsValid, List<string> Errors)
{
    public string ErrorMessage => string.Join(" ", Errors);
}

public class PasswordPolicy
{
    public int MinLength { get; set; } = 8;
    public int MaxLength { get; set; } = 128;
    public bool RequireUppercase { get; set; } = true;
    public bool RequireLowercase { get; set; } = true;
    public bool RequireDigit { get; set; } = true;
    public bool RequireSpecialChar { get; set; } = true;
    public bool RejectCommonPasswords { get; set; } = true;
    public bool RejectRepeatedChars { get; set; } = true;

    public static PasswordPolicy Default => new();

    /// <summary>
    /// Política relaxada para desenvolvimento/testes
    /// </summary>
    public static PasswordPolicy Development => new()
    {
        MinLength = 6,
        RequireUppercase = false,
        RequireLowercase = false,
        RequireDigit = false,
        RequireSpecialChar = false,
        RejectCommonPasswords = false,
        RejectRepeatedChars = false
    };
}
