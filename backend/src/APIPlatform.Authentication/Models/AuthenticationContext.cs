using System.Security.Claims;

namespace APIPlatform.Authentication.Models;

/// <summary>
/// Single context object that flows through every pipeline stage. Each stage enriches it;
/// stages never communicate with each other directly. Mirrors the CrudContext pattern from
/// CrudEngine so the EnterprisePlatform stage model is consistent across all modules.
/// </summary>
public sealed class AuthenticationContext
{
    // ── Input (set by IAuthenticationService before pipeline runs) ──────────
    public required AuthenticationRequest Request { get; init; }
    public CancellationToken CancellationToken { get; init; }

    // ── Stage 1: IdentityResolutionStage ────────────────────────────────────
    public UserInfo? User { get; set; }
    public string? ResolvedIdentityProvider { get; set; }

    // ── Stage 2: ContextEnrichmentStage ─────────────────────────────────────
    public AuthenticationSettings? Settings { get; set; }
    public DeviceInfo? Device { get; set; }
    public DateTimeOffset CurrentTime { get; set; }
    public string? CorrelationId { get; set; }
    public string? RequestId { get; set; }

    // ── Stage 3: ValidationStage ─────────────────────────────────────────────
    public bool ShortCircuited { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }

    // ── Stage 4: AuthenticationPlanningStage ─────────────────────────────────
    public AuthenticationPlan? Plan { get; set; }

    // ── Stage 5: AuthenticationExecutionStage ────────────────────────────────
    public bool PasswordVerified { get; set; }
    public IReadOnlyList<Claim>? GeneratedClaims { get; set; }
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public DateTimeOffset? AccessTokenExpiry { get; set; }
    public DateTimeOffset? RefreshTokenExpiry { get; set; }
    public string? SessionId { get; set; }

    // ── Stage 6: ResponseMappingStage ────────────────────────────────────────
    public AuthenticationResponse? Response { get; set; }

    // ── Cross-cutting ────────────────────────────────────────────────────────
    public IDictionary<string, object?> Diagnostics { get; } = new Dictionary<string, object?>();
}
