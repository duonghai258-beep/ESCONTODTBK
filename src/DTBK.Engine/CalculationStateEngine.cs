using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using DTBK.Domain;

namespace DTBK.Engine;

public sealed class CalculationStateEngine
{
    private const string Version = "2.7.0-calculation-state";
    private readonly NormCostBridge _normCost;
    private readonly ProjectCostEngine _cost;
    public CalculationStateEngine(NormCostBridge normCost, ProjectCostEngine cost) { _normCost = normCost; _cost = cost; }
    public CalculationState Calculate(CalculationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return Calculate(request.Context, request.Items, request.WorkType, request.Scenario, request.Location, request.ProvinceId, request.VatCategory, request.TransportByItem, request.ProvenanceByItem);
    }
    public CalculationState Calculate(CalculationContext context, IReadOnlyList<ProjectItem> items, WorkType workType, CalculationScenario scenario, LocationType location, int provinceId, string vatCategory = "STANDARD", IReadOnlyDictionary<long, decimal>? transportByItem = null, IReadOnlyDictionary<long, CalculationProvenance>? provenanceByItem = null)
    {
        ArgumentNullException.ThrowIfNull(context); ArgumentNullException.ThrowIfNull(items);
        var normResult = _normCost.CalculateWithRequirements(items, provinceId, context.AsOfDate);
        var lines = normResult.Lines; var transport = transportByItem ?? new Dictionary<long, decimal>();
        var summary = _cost.Calculate(lines, workType, scenario, location, context.AsOfDate, vatCategory, transportAmount: transport.Values.Sum());
        var states = lines.Select(line => new CalculationLineState(line, line.Material, line.Labor, line.Machine, transport.TryGetValue(line.ProjectItemID, out var amount) ? amount : 0m, 0m, provenanceByItem is not null && provenanceByItem.TryGetValue(line.ProjectItemID, out var source) ? source : new CalculationProvenance(context.DataPackVersion, "", "", context.AsOfDate, null, context.ProvinceCode, "NormCostBridge → ProjectCostEngine", "Engine default", DTBK.Domain.VerificationStatus.NeedsVerification))).ToList();
        var canonicalTransport = transport.OrderBy(x => x.Key).Select(x => new { ItemId = x.Key, Amount = x.Value }).ToArray();
        var canonicalProvenance = (provenanceByItem ?? new Dictionary<long, CalculationProvenance>()).OrderBy(x => x.Key).Select(x => new { ItemId = x.Key, Provenance = x.Value }).ToArray();
        var inputHash = Hash(new { context, items, workType, scenario, location, provinceId, vatCategory, transport = canonicalTransport, provenance = canonicalProvenance });
        var trace = new CalculationTraceEngine().Create("CALCULATION_STATE", inputHash, Hash(summary), Version, new[] { new CalculationTraceStep("INPUT", "Context", context.ProjectId, context.DataPackVersion, context.DataPackVersion, "S01_CONTEXT"), new CalculationTraceStep("RESOLVER", "NormCostBridge", "ProjectItem → CostLine", context.DataPackVersion, context.DataPackVersion, "S03_RESOLVE"), new CalculationTraceStep("ENGINE", "ProjectCostEngine", "CostLine → ProjectCostSummary", summary.LegalVersion, summary.LegalVersion, "S10_S14_CALCULATE"), new CalculationTraceStep("RESULT", "CalculationState", states.Count.ToString(), context.DataPackVersion, inputHash, "STATE_CREATE"), new CalculationTraceStep("PROJECTION", "T01-T08", "Available from CalculationState", "", inputHash, "STATE_PROJECT") });
        return Capture(context, states, summary, trace, normResult.Requirements);
    }
    public CalculationState Capture(CalculationContext context, IReadOnlyList<CalculationLineState> lines, ProjectCostSummary summary, CalculationTrace trace, IReadOnlyList<ResourceConsumption>? resourceRequirements = null)
    {
        ArgumentNullException.ThrowIfNull(context); ArgumentNullException.ThrowIfNull(lines); ArgumentNullException.ThrowIfNull(summary); ArgumentNullException.ThrowIfNull(trace);
        var status = lines.Any(x => x.Provenance.VerificationStatus != DTBK.Domain.VerificationStatus.Verified) ? DTBK.Domain.VerificationStatus.NeedsVerification : DTBK.Domain.VerificationStatus.Verified;
        return new CalculationState(context, lines, summary, trace, status, status == DTBK.Domain.VerificationStatus.Verified ? Array.Empty<string>() : new[] { "One or more calculation lines lack verified source provenance." }, resourceRequirements ?? Array.Empty<ResourceConsumption>());
    }
    public static CalculationState CapturePricedLines(CalculationContext context, IReadOnlyList<CostLine> lines, ProjectCostSummary summary, DTBK.Domain.VerificationStatus sourceStatus = DTBK.Domain.VerificationStatus.NeedsVerification)
    {
        var states = lines.Select(line => new CalculationLineState(line, line.Material, line.Labor, line.Machine, 0m, 0m, new CalculationProvenance(summary.LegalVersion, "", "", null, null, context.ProvinceCode, "ProjectCostEngine", "Engine default", sourceStatus))).ToList();
        var trace = new CalculationTraceEngine().Create("CALCULATION_STATE", lines.Count.ToString(), summary.TotalConstructionCost.ToString("0.##"), summary.EngineVersion, new[] { new CalculationTraceStep("INPUT", "CostLines", lines.Count.ToString(), "GUI", "", "S02_INPUT"), new CalculationTraceStep("ENGINE", "ProjectCostEngine", "CostLine → ProjectCostSummary", summary.LegalVersion, "", "S10_S14"), new CalculationTraceStep("RESULT", "CalculationState", summary.TotalConstructionCost.ToString("0.##"), summary.LegalVersion, "", "STATE"), new CalculationTraceStep("PROJECTION", "S15", "State-backed outputs", "GUI", "", "S15") });
        var status = states.Any(x => x.Provenance.VerificationStatus != DTBK.Domain.VerificationStatus.Verified) ? DTBK.Domain.VerificationStatus.NeedsVerification : DTBK.Domain.VerificationStatus.Verified;
        return new CalculationState(context, states, summary, trace, status, status == DTBK.Domain.VerificationStatus.Verified ? Array.Empty<string>() : new[] { "One or more calculation lines lack verified source provenance." });
    }
    private static string Hash<T>(T value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(value))));
}
