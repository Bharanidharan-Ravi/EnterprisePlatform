namespace APIPlatform.Authentication.Interfaces;

/// <summary>Applications never hash passwords directly — always through this service.</summary>
public interface IPasswordService
{
    string Hash(string plaintext);
    bool Verify(string plaintext, string hash);
}
