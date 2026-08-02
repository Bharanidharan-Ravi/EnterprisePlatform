namespace APIPlatform.Authentication.Interfaces;

/// <summary>Low-level hashing algorithm abstraction. Replace PBKDF2 with BCrypt/Argon2 by
/// registering a different IPasswordHasher — PasswordService + callers are unaffected.</summary>
public interface IPasswordHasher
{
    string Hash(string plaintext);
    bool Verify(string plaintext, string hash);
}
