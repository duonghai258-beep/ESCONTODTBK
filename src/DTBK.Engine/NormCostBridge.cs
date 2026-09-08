using DTBK.Domain;
using DTBK.Policy;

namespace DTBK.Engine;

/// <summary>Converts project items with NormCode into priced cost lines through NormEngine.</summary>
public sealed class NormCostBridge
{
    private readonly NormEngine _normEngine;
    private readonly RoundingPolicy _rounding;
    public NormCostBridge(NormEngine normEngine, RoundingPolicy? rounding = null) { _normEngine = normEngine; _rounding = rounding ?? new RoundingPolicy(0, "DEFAULT", "Mặc định", 6, 6, 0, RoundingMode.AwayFromZero, true); }
    public IReadOnlyList<CostLine> Calculate(IReadOnlyList<ProjectItem> items, int provinceId, DateTime priceDate) => CalculateWithRequirements(items, provinceId, priceDate).Lines;
    public NormCostBridgeResult CalculateWithRequirements(IReadOnlyList<ProjectItem> items, int provinceId, DateTime priceDate)
    {
        var result = new List<CostLine>(items.Count); var requirements = new List<ResourceConsumption>();
        foreach (var item in items)
        {
            if (string.IsNullOrWhiteSpace(item.NormCode)) throw new InvalidOperationException($"Công tác {item.WorkCode} chưa có mã định mức.");
            var cons = _normEngine.CalculateConsumptionWithPrice(item.NormCode, item.Quantity, provinceId, priceDate, 1m, _rounding); requirements.AddRange(cons);
            decimal mat=0m, lab=0m, mac=0m;
            foreach (var c in cons) { var amount = c.Amount ?? throw new InvalidOperationException($"Chưa có giá cho tài nguyên {c.ResourceCode} của định mức {item.NormCode}."); switch(c.Type) { case ResourceType.Material: mat += amount; break; case ResourceType.Labor: lab += amount; break; case ResourceType.Machine: mac += amount; break; } }
            result.Add(new CostLine(item.ProjectItemID,item.WorkCode,item.Description,item.Unit,item.Quantity,mat,lab,mac,item.NormCode));
        }
        return new NormCostBridgeResult(result, requirements);
    }
}
public sealed record NormCostBridgeResult(IReadOnlyList<CostLine> Lines, IReadOnlyList<ResourceConsumption> Requirements);
