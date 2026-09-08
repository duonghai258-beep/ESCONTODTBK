using DTBK.Domain;

namespace DTBK.Engine;

public sealed class OverrideAwareRateResolver : IRateResolver
{
    private readonly IRateResolver _official;
    private readonly IOverrideResolver _overrides;

    public OverrideAwareRateResolver(IRateResolver official, IOverrideResolver overrides)
    {
        _official = official ?? throw new ArgumentNullException(nameof(official));
        _overrides = overrides ?? throw new ArgumentNullException(nameof(overrides));
    }

    public RateRule Resolve(RateType type, WorkType workType, CalculationScenario scenario, LocationType location, decimal baseValue, DateTime effectiveDate)
    {
        var official = _official.Resolve(type, workType, scenario, location, baseValue, effectiveDate);
        var target = string.IsNullOrWhiteSpace(official.RuleCode) ? type.ToString() : official.RuleCode;
        var overrideValue = _overrides.Resolve(type.ToString(), target, effectiveDate);
        if (overrideValue is null) return official;
        if (!overrideValue.IsApproved) throw new InvalidOperationException($"Override '{overrideValue.OverrideCode}' is not approved.");
        return official with { Rate = overrideValue.Value, RuleCode = $"OVERRIDE:{overrideValue.OverrideCode}" };
    }

    public VATRule ResolveVat(DateTime effectiveDate, string category = "STANDARD")
    {
        var official = _official.ResolveVat(effectiveDate, category);
        var overrideValue = _overrides.Resolve("VAT", category, effectiveDate);
        if (overrideValue is null) return official;
        if (!overrideValue.IsApproved) throw new InvalidOperationException($"Override '{overrideValue.OverrideCode}' is not approved.");
        return official with { Rate = overrideValue.Value };
    }
}
