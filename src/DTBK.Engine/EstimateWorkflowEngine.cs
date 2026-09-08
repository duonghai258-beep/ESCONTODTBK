using System.Security.Cryptography;
using System.Text;
using DTBK.Domain;

namespace DTBK.Engine;

public sealed class EstimateWorkflowEngine
{
    private const string Version = "2.7.0-ESTIMATE-1";
    private readonly NormCostBridge _bridge;
    private readonly ProjectCostEngine _cost;

    public EstimateWorkflowEngine(NormCostBridge bridge, ProjectCostEngine cost)
    { _bridge = bridge ?? throw new ArgumentNullException(nameof(bridge)); _cost = cost ?? throw new ArgumentNullException(nameof(cost)); }

    public EstimateCalculationResult Calculate(IReadOnlyList<ProjectItem> items, WorkType workType, CalculationScenario scenario, LocationType location, int provinceId, DateTime effectiveDate, string vatCategory = "STANDARD")
    {
        if (items is null) throw new ArgumentNullException(nameof(items));
        if (items.Count == 0) throw new InvalidOperationException("Estimate requires at least one project item.");
        var lines = _bridge.Calculate(items, provinceId, effectiveDate);
        var summary = _cost.Calculate(lines, workType, scenario, location, effectiveDate, vatCategory);
        var inputHash = Hash(string.Join("|", items.Select(x => $"{x.ProjectItemID}:{x.WorkCode}:{x.NormCode}:{x.Quantity}")));
        var outputHash = Hash(string.Join("|", lines.Select(x => $"{x.ProjectItemID}:{x.Material}:{x.Labor}:{x.Machine}")));
        return new(lines, summary, new CalculationTrace("ESTIMATE", inputHash, outputHash, Version, new[]
        {
            new CalculationTraceStep("INPUT", "ProjectItems", items.Count.ToString(), "", "", "ESTIMATE_INPUT"),
            new CalculationTraceStep("RESOLVER", "NormCostBridge", "NormCode -> resource prices", "DataPack", summary.LegalVersion, "ESTIMATE_RESOLVE"),
            new CalculationTraceStep("ENGINE", "ProjectCostEngine", "CostLine -> ProjectCostSummary", "", "", "ESTIMATE_CALCULATE"),
            new CalculationTraceStep("OUTPUT", "TotalConstructionCost", summary.TotalConstructionCost.ToString("0.##"), "", outputHash, "ESTIMATE_OUTPUT")
        }));
    }
    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}
