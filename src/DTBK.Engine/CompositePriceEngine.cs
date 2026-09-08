using DTBK.Domain;

namespace DTBK.Engine;

public sealed class CompositePriceEngine
{
    private const string Version = "2.7.0-COMPOSITE-1";
    public CompositePriceResult Calculate(CompositePriceDefinition definition, IReadOnlyDictionary<string, CompositePriceDefinition>? nestedDefinitions = null)
    {
        ArgumentNullException.ThrowIfNull(definition); if (string.IsNullOrWhiteSpace(definition.CompositeCode)) throw new ArgumentException("CompositeCode is required.", nameof(definition)); if (string.IsNullOrWhiteSpace(definition.Unit)) throw new ArgumentException("Composite unit is required.", nameof(definition));
        var definitions = new Dictionary<string, CompositePriceDefinition>(nestedDefinitions ?? new Dictionary<string, CompositePriceDefinition>(), StringComparer.OrdinalIgnoreCase) { [definition.CompositeCode] = definition };
        var expanded = Expand(definition, definitions, new HashSet<string>(StringComparer.OrdinalIgnoreCase)); if (expanded.Any(x => x.Factor < 0m)) throw new InvalidOperationException("Composite component factors cannot be negative."); var total = expanded.Sum(x => x.Amount);
        var trace = new CalculationTrace("COMPOSITE_UNIT_PRICE", definition.CompositeCode, total.ToString("0.######"), Version, new[] { new CalculationTraceStep("INPUT", "Composite", definition.CompositeCode, definition.Provenance.SourceId, definition.Provenance.SourceId, "S09_INPUT"), new CalculationTraceStep("RULE", "Components", expanded.Count.ToString(), definition.Provenance.RuleId, definition.Provenance.SourceId, "S09_COMPONENTS"), new CalculationTraceStep("ENGINE", "CompositePriceEngine", "factor * component unit price", definition.Provenance.RuleId, definition.Provenance.SourceId, "S09_CALCULATE"), new CalculationTraceStep("OUTPUT", "UnitPrice", total.ToString("0.######"), definition.Provenance.SourceId, definition.Provenance.SourceId, "S09_OUTPUT") });
        var status = expanded.Any(x => x.Provenance.VerificationStatus != DTBK.Domain.VerificationStatus.Verified) ? DTBK.Domain.VerificationStatus.NeedsVerification : DTBK.Domain.VerificationStatus.Verified; return new(definition.CompositeCode, definition.Unit, total, expanded, trace, status);
    }
    private static IReadOnlyList<CompositePriceComponent> Expand(CompositePriceDefinition definition, IReadOnlyDictionary<string, CompositePriceDefinition> nested, ISet<string> visiting)
    {
        if (!visiting.Add(definition.CompositeCode)) throw new InvalidOperationException($"Composite price cycle detected at '{definition.CompositeCode}'."); var result = new List<CompositePriceComponent>();
        foreach (var component in definition.Components) { if (string.IsNullOrWhiteSpace(component.ComponentCode)) throw new InvalidOperationException("Composite component code is required."); if (component.Factor < 0m) throw new InvalidOperationException($"Composite component '{component.ComponentCode}' has an invalid factor."); if (nested.TryGetValue(component.ComponentCode, out var nestedDefinition)) { if (!string.Equals(component.Unit, nestedDefinition.Unit, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException($"Composite component unit '{component.Unit}' is incompatible with '{nestedDefinition.Unit}'."); foreach (var child in Expand(nestedDefinition, nested, visiting)) result.Add(child with { Factor = component.Factor * child.Factor }); } else result.Add(component); }
        visiting.Remove(definition.CompositeCode); return result;
    }
}
