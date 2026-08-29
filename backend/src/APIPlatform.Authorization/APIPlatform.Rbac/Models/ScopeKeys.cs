namespace APIPlatform.Rbac.Models;

/// <summary>
/// The scope dimensions <see cref="Contracts.IUserScopeStore"/> understands out of the box. Values
/// match the JWT claim names APIPlatform.Authentication's ClaimsBuilder already emits for
/// UserInfo.DepartmentId/BranchId/CompanyId, so a scope value read from the store and the same
/// value seen in a decoded token are the same key — one convention serves both request-time
/// enforcement and whatever a client reads out of its token.
/// </summary>
public static class ScopeKeys
{
    public const string Department = "department_id";
    public const string Branch = "branch_id";
    public const string Company = "company_id";
}
