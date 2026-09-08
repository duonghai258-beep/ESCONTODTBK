using System.Collections.Generic;

namespace DTBK.Engine;

public sealed record CellDataLink(
    string SheetId, string TableId, string ColumnId, string CellRole, string SourceType,
    string? ProviderOrParser, string? Formula, IReadOnlyList<string> Dependencies,
    string CalculationStatePath, string OutputProjection, string? GuiBinding, string Status, string Reason);

public static partial class CanonicalCellDataLinkRegistry
{
    private static readonly HashSet<string> Calculated = new(StringComparer.OrdinalIgnoreCase)
    {
        "AMOUNT", "UNIT_PRICE", "MATERIAL_COST", "LABOR_COST", "MACHINE_COST", "TRANSPORT_COST",
        "OTHER_COST", "DIRECT_COST", "POST_TAX", "VAT", "VALUE", "BASIS", "COST", "QUANTITY",
        "LEVEL", "COEFFICIENT", "PRICE", "RATE", "LOAD", "FUEL_FACTOR", "LABOR_FACTOR", "WASTE_LOSS"
    };

    public static CellDataLink Resolve(string sheetId, string tableId, string rowKey, string columnId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sheetId); ArgumentException.ThrowIfNullOrWhiteSpace(tableId);
        ArgumentException.ThrowIfNullOrWhiteSpace(rowKey); ArgumentException.ThrowIfNullOrWhiteSpace(columnId);
        var c = columnId.ToUpperInvariant(); var s = sheetId.ToUpperInvariant(); var t = tableId.ToUpperInvariant();
        if (s == "S01") return S01(t, c); if (s == "S02") return S02(t, c); if (s == "S03") return S03(t, c);
        if (s == "S04") return Generic(s, t, c, "COEFFICIENT_PROVIDER", "S04"); if (s == "S05") return S05(t, c);
        if (s == "S06") return S06(t, c); if (s == "S07") return S07(t, c); if (s == "S08") return S08(t, c);
        if (s == "S09") return S09(t, c); if (s == "S10") return Explain(s, t, c);
        if (s == "S11") return Generic(s, t, c, "AUDIT_STATE", "S11"); if (s == "S12") return Generic(s, t, c, "CALCULATION_STATE", "S12");
        if (s == "S13") return Generic(s, t, c, "CALCULATION_STATE", "S13"); if (s == "S14") return Generic(s, t, c, "COMMERCIAL_WORKFLOW", "S14");
        if (s == "S15") return Generic(s, t, c, "CALCULATION_STATE", "S15"); return Missing(s, t, c, "Unknown canonical sheet.");
    }

    private static CellDataLink S01(string t, string c) => t switch
    {
        "BOQ" when c == "WORK_CODE" => Lookup("S01", t, c, "NORM_PROVIDER", "S08.NORMS", "S05.UNIT_PRICE", "NORM_SELECTION", "GUI:S01.BOQ.WORK_CODE"),
        "BOQ" when c == "QUANTITY" => Input("S01", t, c, "TAKEOFF_IMPORTER|USER", "S02.TAKEOFF", "S01.BOQ.AMOUNT", "GUI:S01.BOQ.QUANTITY"),
        "BOQ" when c == "UNIT_PRICE_REF" => Lookup("S01", t, c, "PRICE_PROVIDER", "S05.UNIT_PRICE", "S05.UNIT_PRICE.UNIT_PRICE", "GUI:S01.BOQ.UNIT_PRICE_REF"),
        "BOQ" when c == "AMOUNT" => Calc("S01", t, c, "QUANTITY * UNIT_PRICE_REF", new[] { "S01.BOQ.QUANTITY", "S01.BOQ.UNIT_PRICE_REF" }, "S03.SUMMARY"),
        "BOQ" when c == "NORM_CODE" => Lookup("S01", t, c, "NORM_PROVIDER", "S08.NORMS", "S05.UNIT_PRICE", "GUI:S01.BOQ.NORM_CODE"),
        "QUANTITY_DETAIL" when c == "QUANTITY" => Calc("S01", t, c, "L1 * L2 * L3 * L4 * FACTOR", new[] { "L1", "L2", "L3", "L4", "FACTOR" }, "S01.BOQ.QUANTITY"),
        _ => Generic("S01", t, c, "IMPORTER|USER|PROJECT_DATA", "S01")
    };
    private static CellDataLink S02(string t, string c) => c == "QUANTITY" ? Calc("S02", t, c, "L1 * L2 * L3 * L4 * FACTOR", new[] { "L1", "L2", "L3", "L4", "FACTOR" }, "S01.BOQ.QUANTITY") : Generic("S02", t, c, "TAKEOFF_IMPORTER|USER|PROJECT_DATA", "S02");
    private static CellDataLink S03(string t, string c) => c switch
    {
        "PRE_TAX" => Projection("S03", t, c, "CALCULATION_STATE", "S01/S05/S06/S09", "S03.SUMMARY.PRE_TAX"),
        "VAT" => Calc("S03", t, c, "BASIS * RATE", new[] { "BASIS", "RATE" }, "S03.SUMMARY.VAT"),
        "POST_TAX" => Calc("S03", t, c, "PRE_TAX + VAT", new[] { "PRE_TAX", "VAT" }, "S03.SUMMARY.POST_TAX"),
        "VALUE" => Projection("S03", t, c, "CALCULATION_STATE", "S01/S05/S06/S09", "S03.COST_COMPONENT.VALUE"),
        _ => Generic("S03", t, c, "CALCULATION_STATE", "S03")
    };
    private static CellDataLink S05(string t, string c) => t switch
    {
        "UNIT_PRICE" when c == "MATERIAL_COST" => Calc("S05", t, c, "SUM(MATERIAL_COMPONENT.AMOUNT)", new[] { "S05.MATERIAL_COMPONENT.AMOUNT" }, "S05.UNIT_PRICE.MATERIAL_COST"),
        "UNIT_PRICE" when c == "LABOR_COST" => Calc("S05", t, c, "SUM(LABOR_COMPONENT.AMOUNT)", new[] { "S05.LABOR_COMPONENT.AMOUNT" }, "S05.UNIT_PRICE.LABOR_COST"),
        "UNIT_PRICE" when c == "MACHINE_COST" => Calc("S05", t, c, "SUM(MACHINE_COMPONENT.AMOUNT)", new[] { "S05.MACHINE_COMPONENT.AMOUNT" }, "S05.UNIT_PRICE.MACHINE_COST"),
        "UNIT_PRICE" when c == "TRANSPORT_COST" => Calc("S05", t, c, "SUM(TRANSPORT_COMPONENT.AMOUNT)", new[] { "S05.TRANSPORT_COMPONENT.AMOUNT" }, "S05.UNIT_PRICE.TRANSPORT_COST"),
        "UNIT_PRICE" when c == "DIRECT_COST" => Calc("S05", t, c, "MATERIAL_COST + LABOR_COST + MACHINE_COST + TRANSPORT_COST + OTHER_COST", new[] { "MATERIAL_COST", "LABOR_COST", "MACHINE_COST", "TRANSPORT_COST", "OTHER_COST" }, "S03.COST_COMPONENT"),
        "UNIT_PRICE" when c == "UNIT_PRICE" => Calc("S05", t, c, "DIRECT_COST", new[] { "DIRECT_COST" }, "S01.BOQ.UNIT_PRICE_REF"),
        "UNIT_PRICE" when c == "AMOUNT" => Calc("S05", t, c, "QUANTITY * UNIT_PRICE", new[] { "QUANTITY", "UNIT_PRICE" }, "S03.SUMMARY"),
        "MATERIAL_COMPONENT" when c == "COEFFICIENT" => Lookup("S05", t, c, "NORM_PROVIDER", "S08.NORM_DETAIL", "S06.RESOURCE_REQUIREMENTS", "NORM_REF"),
        "MATERIAL_COMPONENT" when c == "PRICE" => Lookup("S05", t, c, "MATERIAL_PRICE_PROVIDER", "S07.MATERIAL_PRICE", "S05.MATERIAL_COMPONENT.AMOUNT", "PRICE_REF"),
        "MATERIAL_COMPONENT" when c == "AMOUNT" => Calc("S05", t, c, "COEFFICIENT * PRICE", new[] { "COEFFICIENT", "PRICE" }, "S05.UNIT_PRICE.MATERIAL_COST"),
        "LABOR_COMPONENT" when c == "COEFFICIENT" => Lookup("S05", t, c, "NORM_PROVIDER", "S08.NORM_DETAIL", "S06.LABOR", "NORM_REF"),
        "LABOR_COMPONENT" when c == "PRICE" => Lookup("S05", t, c, "LABOR_PRICE_PROVIDER", "S07.LABOR_PRICE", "S05.LABOR_COMPONENT.AMOUNT", "PRICE_REF"),
        "LABOR_COMPONENT" when c == "AMOUNT" => Calc("S05", t, c, "COEFFICIENT * PRICE", new[] { "COEFFICIENT", "PRICE" }, "S05.UNIT_PRICE.LABOR_COST"),
        "MACHINE_COMPONENT" when c == "COEFFICIENT" => Lookup("S05", t, c, "NORM_PROVIDER", "S08.NORM_DETAIL", "S06.MACHINE", "NORM_REF"),
        "MACHINE_COMPONENT" when c == "PRICE" => Lookup("S05", t, c, "MACHINE_PRICE_PROVIDER", "S07.MACHINE_PRICE", "S05.MACHINE_COMPONENT.AMOUNT", "PRICE_REF"),
        "MACHINE_COMPONENT" when c == "AMOUNT" => Calc("S05", t, c, "COEFFICIENT * PRICE", new[] { "COEFFICIENT", "PRICE" }, "S05.UNIT_PRICE.MACHINE_COST"),
        "TRANSPORT_COMPONENT" when c == "AMOUNT" => Calc("S05", t, c, "DISTANCE * RATE", new[] { "DISTANCE", "RATE" }, "S05.UNIT_PRICE.TRANSPORT_COST"),
        "TRANSPORT_COMPONENT" when c == "RATE" => Lookup("S05", t, c, "TRANSPORT_PROVIDER", "S09.TRANSPORT", "S05.TRANSPORT_COMPONENT.AMOUNT", "TRANSPORT_CODE"),
        _ => Generic("S05", t, c, "NORM_PROVIDER|PRICE_PROVIDER|CALCULATION_STATE", "S05")
    };
    private static CellDataLink S06(string t, string c) => c switch
    {
        "COEFFICIENT" => Lookup("S06", t, c, "NORM_PROVIDER", "S08.NORM_DETAIL", "S06.QUANTITY", "NORM_REF"),
        "QUANTITY" => Calc("S06", t, c, "COEFFICIENT * S01.BOQ.QUANTITY", new[] { "COEFFICIENT", "S01.BOQ.QUANTITY" }, "S05 component quantities"),
        "PRICE_REF" => Lookup("S06", t, c, "PRICE_PROVIDER", "S07.RESOURCE_PRICES", "S06.AMOUNT", "RESOURCE_CODE"),
        "AMOUNT" => Calc("S06", t, c, "QUANTITY * PRICE_REF", new[] { "QUANTITY", "PRICE_REF" }, "S05 components"),
        _ => Generic("S06", t, c, "NORM_PROVIDER|PRICE_PROVIDER|CALCULATION_STATE", "S06")
    };
    private static CellDataLink S07(string t, string c) => c switch
    {
        "PRICE" or "RATE" => Input("S07", t, c, "PRICE_IMPORTER|USER|LEGAL_DATAPACK", "S05/S06/S09", "GUI:S07.PRICE"),
        _ => Generic("S07", t, c, "PRICE_IMPORTER|LEGAL_DATAPACK|USER", "S07")
    };
    private static CellDataLink S08(string t, string c) => c switch
    {
        "COEFFICIENT" => Input("S08", t, c, "NORM_IMPORTER|LEGAL_DATAPACK", "S05/S06", "GUI:S08.NORM_DETAIL.COEFFICIENT"),
        _ => Generic("S08", t, c, "NORM_IMPORTER|LEGAL_DATAPACK|USER", "S08")
    };
    private static CellDataLink S09(string t, string c) => c switch
    {
        "DISTANCE" => Input("S09", t, c, "TRANSPORT_IMPORTER|USER|TAKEOFF", "S09.COST", "GUI:S09.DISTANCE"),
        "RATE" => Lookup("S09", t, c, "TRANSPORT_PROVIDER", "S09.RATE_BAND", "S09.COST", "ROUTE_TYPE/DISTANCE"),
        "COST" => Calc("S09", t, c, "DISTANCE * RATE * FUEL_FACTOR", new[] { "DISTANCE", "RATE", "FUEL_FACTOR" }, "S05/S03"),
        _ => Generic("S09", t, c, "TRANSPORT_PROVIDER|TRANSPORT_IMPORTER|USER", "S09")
    };
    private static CellDataLink Explain(string s, string t, string c) => Projection(s, t, c, "CALCULATION_STATE + PROVENANCE", "all upstream calculation cells", "S10.EXPLAIN");
    private static CellDataLink Input(string s, string t, string c, string source, string output, string dependency, string gui) => new(s, t, c, "Input", source, null, null, new[] { dependency }, "INPUT→STATE", output, gui, "CONTRACT_ONLY", "Concrete provider/control wiring must be verified by source audit.");
    private static CellDataLink Lookup(string s, string t, string c, string provider, string deps, string output, string dependency, string gui) => new(s, t, c, "Lookup", "Provider", provider, null, new[] { deps, dependency }, "LOOKUP→STATE", output, gui, "CONTRACT_ONLY", "Provider is identified by contract; runtime implementation must be verified.");
    private static CellDataLink Calc(string s, string t, string c, string formula, IReadOnlyList<string> deps, string output) => new(s, t, c, "Calculated", "CalculationState", null, formula, deps, "CalculationStateEngine.Calculate", output, null, "CONTRACT_ONLY", "Formula/dependency is canonical contract; runtime formula implementation must be verified.");
    private static CellDataLink Projection(string s, string t, string c, string source, string deps, string output) => new(s, t, c, "DisplayOnly", source, null, null, new[] { deps }, "CalculationState", output, null, "CONTRACT_ONLY", "Projection must consume CalculationState rather than recalculate.");
    private static CellDataLink Generic(string s, string t, string c, string source, string output) => new(s, t, c, Calculated.Contains(c) ? "Derived" : "Input", source, null, null, Array.Empty<string>(), "CalculationState", output, null, "CONTRACT_ONLY", "Column has a source family but no concrete cell-level provider/formula has been proven.");
    private static CellDataLink Missing(string s, string t, string c, string reason) => new(s, t, c, "Unknown", "", null, null, Array.Empty<string>(), "", "", null, "MISSING", reason);
}
