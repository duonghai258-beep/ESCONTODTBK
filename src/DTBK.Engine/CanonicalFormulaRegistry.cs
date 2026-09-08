namespace DTBK.Engine;

public sealed record CanonicalFormula(
    string FormulaId,
    string TargetPattern,
    string Expression,
    IReadOnlyList<string> DependencyPatterns,
    string OutputProjection);

/// <summary>
/// Canonical formula/dependency metadata for the workbook. It registers the exact
/// row-scoped edges used by invalidation; actual estimation calculation remains owned
/// by CalculationStateEngine.Calculate().
/// </summary>
public static class CanonicalFormulaRegistry
{
    public static IReadOnlyList<CanonicalFormula> Definitions { get; } = new[]
    {
        new CanonicalFormula("F-S01-BOQ-AMOUNT", "S01/BOQ/{row}/AMOUNT", "QUANTITY * UNIT_PRICE_REF", new[] { "S01/BOQ/{row}/QUANTITY", "S01/BOQ/{row}/UNIT_PRICE_REF" }, "S03.SUMMARY"),
        new CanonicalFormula("F-S01-QUANTITY-DETAIL", "S01/QUANTITY_DETAIL/{row}/QUANTITY", "L1 * L2 * L3 * L4 * FACTOR", new[] { "S01/QUANTITY_DETAIL/{row}/L1", "S01/QUANTITY_DETAIL/{row}/L2", "S01/QUANTITY_DETAIL/{row}/L3", "S01/QUANTITY_DETAIL/{row}/L4", "S01/QUANTITY_DETAIL/{row}/FACTOR" }, "S01/BOQ/{row}/QUANTITY"),
        new CanonicalFormula("F-S05-MATERIAL-AMOUNT", "S05/MATERIAL_COMPONENT/{row}/AMOUNT", "COEFFICIENT * PRICE", new[] { "S05/MATERIAL_COMPONENT/{row}/COEFFICIENT", "S05/MATERIAL_COMPONENT/{row}/PRICE" }, "S05/UNIT_PRICE/{row}/MATERIAL_COST"),
        new CanonicalFormula("F-S05-LABOR-AMOUNT", "S05/LABOR_COMPONENT/{row}/AMOUNT", "COEFFICIENT * PRICE", new[] { "S05/LABOR_COMPONENT/{row}/COEFFICIENT", "S05/LABOR_COMPONENT/{row}/PRICE" }, "S05/UNIT_PRICE/{row}/LABOR_COST"),
        new CanonicalFormula("F-S05-MACHINE-AMOUNT", "S05/MACHINE_COMPONENT/{row}/AMOUNT", "COEFFICIENT * PRICE", new[] { "S05/MACHINE_COMPONENT/{row}/COEFFICIENT", "S05/MACHINE_COMPONENT/{row}/PRICE" }, "S05/UNIT_PRICE/{row}/MACHINE_COST"),
        new CanonicalFormula("F-S05-TRANSPORT-AMOUNT", "S05/TRANSPORT_COMPONENT/{row}/AMOUNT", "DISTANCE * RATE", new[] { "S05/TRANSPORT_COMPONENT/{row}/DISTANCE", "S05/TRANSPORT_COMPONENT/{row}/RATE" }, "S05/UNIT_PRICE/{row}/TRANSPORT_COST"),
        new CanonicalFormula("F-S05-DIRECT-COST", "S05/UNIT_PRICE/{row}/DIRECT_COST", "MATERIAL_COST + LABOR_COST + MACHINE_COST + TRANSPORT_COST + OTHER_COST", new[] { "S05/UNIT_PRICE/{row}/MATERIAL_COST", "S05/UNIT_PRICE/{row}/LABOR_COST", "S05/UNIT_PRICE/{row}/MACHINE_COST", "S05/UNIT_PRICE/{row}/TRANSPORT_COST", "S05/UNIT_PRICE/{row}/OTHER_COST" }, "S03/COST_COMPONENT/{row}/VALUE"),
        new CanonicalFormula("F-S05-UNIT-PRICE", "S05/UNIT_PRICE/{row}/UNIT_PRICE", "DIRECT_COST", new[] { "S05/UNIT_PRICE/{row}/DIRECT_COST" }, "S01/BOQ/{row}/UNIT_PRICE_REF"),
        new CanonicalFormula("F-S05-AMOUNT", "S05/UNIT_PRICE/{row}/AMOUNT", "QUANTITY * UNIT_PRICE", new[] { "S05/UNIT_PRICE/{row}/QUANTITY", "S05/UNIT_PRICE/{row}/UNIT_PRICE" }, "S03/SUMMARY/{row}/VALUE"),
        new CanonicalFormula("F-S06-RESOURCE-QUANTITY", "S06/RESOURCE_REQUIREMENTS/{row}/QUANTITY", "COEFFICIENT * S01/BOQ/{row}/QUANTITY", new[] { "S06/RESOURCE_REQUIREMENTS/{row}/COEFFICIENT", "S01/BOQ/{row}/QUANTITY" }, "S05 component quantities"),
        new CanonicalFormula("F-S06-RESOURCE-AMOUNT", "S06/RESOURCE_REQUIREMENTS/{row}/AMOUNT", "QUANTITY * PRICE_REF", new[] { "S06/RESOURCE_REQUIREMENTS/{row}/QUANTITY", "S06/RESOURCE_REQUIREMENTS/{row}/PRICE_REF" }, "S05 components"),
        new CanonicalFormula("F-S09-TRANSPORT-COST", "S09/TRANSPORT/{row}/COST", "DISTANCE * RATE * FUEL_FACTOR", new[] { "S09/TRANSPORT/{row}/DISTANCE", "S09/TRANSPORT/{row}/RATE", "S09/TRANSPORT/{row}/FUEL_FACTOR" }, "S05/S03"),
        new CanonicalFormula("F-S03-VAT", "S03/SUMMARY/{row}/VAT", "BASIS * RATE", new[] { "S03/SUMMARY/{row}/BASIS", "S03/SUMMARY/{row}/RATE" }, "S03/SUMMARY/{row}/VAT"),
        new CanonicalFormula("F-S03-POST-TAX", "S03/SUMMARY/{row}/POST_TAX", "PRE_TAX + VAT", new[] { "S03/SUMMARY/{row}/PRE_TAX", "S03/SUMMARY/{row}/VAT" }, "S03/SUMMARY/{row}/POST_TAX")
    };

    public static void Register(RecalculationScheduler scheduler, string rowKey)
    {
        ArgumentNullException.ThrowIfNull(scheduler);
        ArgumentException.ThrowIfNullOrWhiteSpace(rowKey);
        foreach (var formula in Definitions)
        {
            var target = formula.TargetPattern.Replace("{row}", rowKey, StringComparison.Ordinal);
            var dependencies = formula.DependencyPatterns.Select(x => x.Replace("{row}", rowKey, StringComparison.Ordinal));
            scheduler.AddFormulaDependencies(target, dependencies);
        }

        scheduler.AddDependency($"S08/NORM_DETAIL/{rowKey}/COEFFICIENT", $"S05/MATERIAL_COMPONENT/{rowKey}/COEFFICIENT", "LOOKUP");
        scheduler.AddDependency($"S08/NORM_DETAIL/{rowKey}/COEFFICIENT", $"S05/LABOR_COMPONENT/{rowKey}/COEFFICIENT", "LOOKUP");
        scheduler.AddDependency($"S08/NORM_DETAIL/{rowKey}/COEFFICIENT", $"S05/MACHINE_COMPONENT/{rowKey}/COEFFICIENT", "LOOKUP");
        scheduler.AddDependency($"S07/MATERIAL_PRICE/{rowKey}/PRICE", $"S05/MATERIAL_COMPONENT/{rowKey}/PRICE", "LOOKUP");
        scheduler.AddDependency($"S07/LABOR_PRICE/{rowKey}/PRICE", $"S05/LABOR_COMPONENT/{rowKey}/PRICE", "LOOKUP");
        scheduler.AddDependency($"S07/MACHINE_PRICE/{rowKey}/PRICE", $"S05/MACHINE_COMPONENT/{rowKey}/PRICE", "LOOKUP");
        scheduler.AddDependency($"S09/TRANSPORT/{rowKey}/RATE", $"S05/TRANSPORT_COMPONENT/{rowKey}/RATE", "LOOKUP");
        scheduler.AddDependency($"S09/TRANSPORT/{rowKey}/DISTANCE", $"S05/TRANSPORT_COMPONENT/{rowKey}/DISTANCE", "LOOKUP");
    }
}
