namespace DTBK.Engine;

/// <summary>
/// Integrates an evidence-gated formula result with the existing dependency scheduler without taking ownership of calculation. The canonical calculation engine remains responsible for evaluating project costs.
/// </summary>
public static class FormulaRuntimeIntegration
{
    public static FormulaRuntimeRegistration Register(RecalculationScheduler scheduler, string target, FormulaFunctionResult result)
    {
        ArgumentNullException.ThrowIfNull(scheduler);
        ArgumentException.ThrowIfNullOrWhiteSpace(target);
        ArgumentNullException.ThrowIfNull(result);
        var verificationStatus = result.ValueResult.VerificationStatus;
        var canonicalProvenance = result.ValueResult.Provenance?.TryCreateCanonical(verificationStatus);
        scheduler.RegisterFormulaResult(target, result.ValueResult);
        return new FormulaRuntimeRegistration(target, result.CanonicalFunctionName ?? result.ValueResult.Trace.FunctionName, verificationStatus, result.ValueResult.Dependencies, result.ValueResult.Provenance, canonicalProvenance, result.ValueResult.Trace, result.ValueResult.ErrorCode, result.ValueResult.ErrorMessage);
    }
}

/// <summary>
/// Immutable audit projection of a formula-runtime evaluation. Canonical provenance is populated only when all required canonical fields are explicitly evidenced; otherwise it remains null rather than being inferred from source/version labels.
/// </summary>
public sealed record FormulaRuntimeRegistration(
    string Target,
    string FunctionName,
    DTBK.Domain.VerificationStatus VerificationStatus,
    IReadOnlyList<FormulaDependency> Dependencies,
    FormulaProvenance? Provenance,
    DTBK.Domain.CalculationProvenance? CanonicalProvenance,
    FormulaTraceStep Trace,
    string? ErrorCode,
    string? ErrorMessage);
