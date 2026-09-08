namespace DTBK.Engine;

/// <summary>
/// P00 engine boundary: keeps the ESCON reconstruction explicit about its scope,
/// phase ordering and evidence state without introducing UI or guessed data access.
/// </summary>
public static class EsconEngineContract
{
    public const string Scope = "ENGINE_ONLY";
    public const string SourceOfTruth = "ESCONTODTBK + ESCON forensic evidence";

    public static CalculationContextGuard RequireContext(DTBK.Domain.CalculationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return new CalculationContextGuard(context);
    }

    public static string CanonicalFunctionName(string functionName)
    {
        if (string.IsNullOrWhiteSpace(functionName))
            throw new ArgumentException("Function name is required.", nameof(functionName));
        return functionName.Trim().ToUpperInvariant() switch
        {
            "SPVALUE" => "SPVALUE",
            "SUMGROUP" => "SUMGROUP",
            "IF" => "IF",
            _ => functionName.Trim()
        };
    }
}

public sealed record CalculationContextGuard(DTBK.Domain.CalculationContext Context)
{
    public EsconFormulaRuntime BindFormulaRuntime(
        IEsconValueProvider provider,
        IReadOnlyList<IReadOnlyDictionary<string, decimal?>>? groupRows = null,
        SpecialValueStore? specialValues = null)
    {
        ArgumentNullException.ThrowIfNull(provider);
        return new EsconFormulaRuntime(provider, groupRows, specialValues);
    }
}
