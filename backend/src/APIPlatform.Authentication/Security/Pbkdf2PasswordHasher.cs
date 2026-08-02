using System.Security.Cryptography;
using APIPlatform.Authentication.Interfaces;

namespace APIPlatform.Authentication.Security;

/// <summary>PBKDF2-SHA512 password hasher. Constant-time comparison prevents timing attacks.
/// Replace with BCrypt/Argon2 by registering a different IPasswordHasher — callers are
/// unaffected.</summary>
public sealed class Pbkdf2PasswordHasher : IPasswordHasher
{
    private const int SaltSize = 16;
    private const int KeySize = 32;
    private const int Iterations = 310_000;
    private static readonly HashAlgorithmName Algorithm = HashAlgorithmName.SHA512;

    public string Hash(string plaintext)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var key = Rfc2898DeriveBytes.Pbkdf2(plaintext, salt, Iterations, Algorithm, KeySize);
        return $"v1${Convert.ToBase64String(salt)}${Convert.ToBase64String(key)}";
    }

    public bool Verify(string plaintext, string hash)
    {
        var parts = hash.Split('$');
        if (parts.Length != 3 || parts[0] != "v1") return false;
        try
        {
            var salt = Convert.FromBase64String(parts[1]);
            var storedKey = Convert.FromBase64String(parts[2]);
            var derivedKey = Rfc2898DeriveBytes.Pbkdf2(plaintext, salt, Iterations, Algorithm, KeySize);
            return CryptographicOperations.FixedTimeEquals(derivedKey, storedKey);
        }
        catch { return false; }
    }
}
