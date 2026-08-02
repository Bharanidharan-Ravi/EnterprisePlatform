namespace APIPlatform.Foundation.Results;

/// <summary>Outcome of a validation pass, shared by APIPlatform.Validation and UIPlatform.Forms.</summary>
public sealed record ValidationResult : IResult
{
    public required bool IsValid { get; init; }
    public IReadOnlyList<ErrorInfo> Errors { get; init; } = Array.Empty<ErrorInfo>();

    /// <summary>Satisfies IResult; mirrors <see cref="IsValid"/> so ValidationResult composes with the shared result contract.</summary>
    bool IResult.Succeeded => IsValid;

    public static ValidationResult Valid() => new() { IsValid = true };
    public static ValidationResult Invalid(params ErrorInfo[] errors) => new() { IsValid = false, Errors = errors };
}
