namespace APIPlatform.Foundation;

/// <summary>
/// STUB — placeholder for the real APIPlatform.Foundation package (frozen, not part of this
/// codebase yet). Defines only the minimal surface APIPlatform.Rbac needs to compile and run.
/// Replace this project reference with the real Foundation package when it exists; the
/// interface shape below is taken directly from Nucleus_Master_Plan_Rev2.pdf, Section 3.1/3.2.
/// </summary>
public interface ICurrentUser
{
    string UserId { get; }
    bool IsAuthenticated { get; }
}
