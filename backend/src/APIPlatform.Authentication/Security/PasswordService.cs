using APIPlatform.Authentication.Interfaces;

namespace APIPlatform.Authentication.Security;

public sealed class PasswordService : IPasswordService
{
    private readonly IPasswordHasher _hasher;
    public PasswordService(IPasswordHasher hasher) => _hasher = hasher;
    public string Hash(string plaintext) => _hasher.Hash(plaintext);
    public bool Verify(string plaintext, string hash) => _hasher.Verify(plaintext, hash);
}
