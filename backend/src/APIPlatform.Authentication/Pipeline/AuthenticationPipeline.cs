using APIPlatform.Authentication.Models;
using APIPlatform.Authentication.Pipeline.Stages;

namespace APIPlatform.Authentication.Pipeline;

/// <summary>
/// Orchestrates the fixed enterprise authentication sequence. Contains no logic — only
/// stage sequencing. Mirrors CrudPipeline's pattern so the EnterprisePlatform execution
/// model is consistent across all modules.
///
///   IdentityResolution → ContextEnrichment → Validation
///     → AuthenticationPlanning → AuthenticationExecution → ResponseMapping
/// </summary>
public sealed class AuthenticationPipeline : IAuthenticationPipeline
{
    private readonly IdentityResolutionStage      _identity;
    private readonly ContextEnrichmentStage       _enrichment;
    private readonly ValidationStage              _validation;
    private readonly AuthenticationPlanningStage  _planning;
    private readonly AuthenticationExecutionStage _execution;
    private readonly ResponseMappingStage         _mapping;

    public AuthenticationPipeline(
        IdentityResolutionStage      identity,
        ContextEnrichmentStage       enrichment,
        ValidationStage              validation,
        AuthenticationPlanningStage  planning,
        AuthenticationExecutionStage execution,
        ResponseMappingStage         mapping)
    {
        _identity   = identity;
        _enrichment = enrichment;
        _validation = validation;
        _planning   = planning;
        _execution  = execution;
        _mapping    = mapping;
    }

    public async Task<AuthenticationContext> RunAsync(AuthenticationContext context)
    {
        await _identity.ExecuteAsync(context);
        await _enrichment.ExecuteAsync(context);
        await _validation.ExecuteAsync(context);

        if (context.ShortCircuited)
        {
            await _mapping.ExecuteAsync(context);
            return context;
        }

        await _planning.ExecuteAsync(context);
        await _execution.ExecuteAsync(context);
        await _mapping.ExecuteAsync(context);
        return context;
    }
}
