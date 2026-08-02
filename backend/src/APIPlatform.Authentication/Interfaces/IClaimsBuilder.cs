using System.Security.Claims;
using APIPlatform.Authentication.Models;

namespace APIPlatform.Authentication.Interfaces;

/// <summary>Builds the Claim list from resolved user context. Register additional
/// IClaimsBuilderExtension implementations to add app-specific claims without modifying this
/// interface.</summary>
public interface IClaimsBuilder
{
    IReadOnlyList<Claim> Build(AuthenticationContext context);
}

/// <summary>Extension point — app-specific claims (DbName, Branch, AppVersion, etc.) are added
/// via IClaimsBuilderExtension so the core IClaimsBuilder stays domain-free.</summary>
public interface IClaimsBuilderExtension
{
    IEnumerable<Claim> Extend(AuthenticationContext context);
}
