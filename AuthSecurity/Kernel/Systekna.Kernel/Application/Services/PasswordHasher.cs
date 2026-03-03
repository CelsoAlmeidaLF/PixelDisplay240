using Systekna.Kernel.Domain.Interfaces;

namespace Systekna.Kernel.Old.Application.Services;

public class PasswordHasher : IPasswordHasher
{
    public string Hash(string password) => BCrypt.Net.BCrypt.HashPassword(password);
    public bool Verify(string password, string hash) => BCrypt.Net.BCrypt.Verify(password, hash);
}
