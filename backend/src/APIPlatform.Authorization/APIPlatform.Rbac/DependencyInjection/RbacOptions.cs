namespace APIPlatform.Rbac.DependencyInjection;

public sealed class RbacOptions
{
    /// <summary>
    /// Documents the platform's default-deny posture (see PermissionEvaluator/ExecutionStage
    /// remarks). Currently informational only — the pipeline is always default-deny; this
    /// flag is reserved for a future explicit opt-out, not implemented for the v1 checkpoint.
    /// </summary>
    public bool DefaultDeny { get; set; } = true;

    public TimeSpan PermissionCacheTtl { get; set; } = TimeSpan.FromMinutes(5);
}
