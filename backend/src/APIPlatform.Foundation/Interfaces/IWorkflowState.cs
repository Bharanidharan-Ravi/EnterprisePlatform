namespace APIPlatform.Foundation.Interfaces;

/// <summary>
/// Marker contract for an entity participating in the generic Workflow engine (Section 3.11).
/// Carries no domain meaning — state names (Draft, Approved, Running, Completed, etc.) are
/// entirely config-driven by the consuming application.
/// </summary>
public interface IWorkflowState
{
    /// <summary>The entity's current workflow state, as defined by the consuming app's config — not necessarily a "stage" in the approval-chain sense.</summary>
    string CurrentState { get; }
}
