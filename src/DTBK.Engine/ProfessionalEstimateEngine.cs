using DTBK.Domain;

namespace DTBK.Engine;

/// <summary>
/// Professional estimating workflow assembled from broadly observed industry patterns:
/// separated QTO/MTO/cost layers, resource traceability, bid scenarios, review gates,
/// revision comparison and auditable cost structures. It deliberately does not copy
/// proprietary code, templates or databases from third parties.
/// </summary>
public sealed class ProfessionalEstimateEngine
{
    public ProfessionalEstimateReview Review(IReadOnlyList<CostLine> lines, ProjectCostSummary? summary)
    {
        var issues = new List<ProfessionalIssue>();
        if (lines.Count == 0)
            issues.Add(new("EMPTY_BOQ", "error", "Chưa có công tác trong bảng tiên lượng."));

        foreach (var line in lines)
        {
            if (line.Quantity < 0)
                issues.Add(new("NEGATIVE_QTY", "error", $"Khối lượng âm: {line.WorkCode}"));
            if (string.IsNullOrWhiteSpace(line.WorkCode))
                issues.Add(new("MISSING_WORK_CODE", "error", "Có dòng thiếu mã công tác."));
            if (string.IsNullOrWhiteSpace(line.Unit))
                issues.Add(new("MISSING_UNIT", "warning", $"Thiếu đơn vị: {line.WorkCode}"));
            if (string.IsNullOrWhiteSpace(line.NormCode))
                issues.Add(new("NO_NORM", "warning", $"Chưa gắn định mức: {line.WorkCode}"));
            if (line.Material < 0 || line.Labor < 0 || line.Machine < 0)
                issues.Add(new("NEGATIVE_COMPONENT", "error", $"Chi phí thành phần âm: {line.WorkCode}"));
            if (line.Quantity > 0 && line.Material == 0 && line.Labor == 0 && line.Machine == 0)
                issues.Add(new("ZERO_PRICE", "warning", $"Công tác có khối lượng nhưng chưa có chi phí: {line.WorkCode}"));
        }

        if (summary is null)
            issues.Add(new("NO_CALCULATION", "warning", "Chưa có kết quả tổng hợp."));
        else if (summary.TotalConstructionCost < 0)
            issues.Add(new("NEGATIVE_TOTAL", "error", "Tổng chi phí âm."));

        return new ProfessionalEstimateReview(issues);
    }

    public ProfessionalCostBreakdown BuildBreakdown(ProjectCostSummary summary) =>
        new(
            summary.DirectMaterial,
            summary.DirectLabor,
            summary.DirectMachine,
            summary.DirectCost,
            summary.Overhead,
            summary.TemporaryHouse,
            summary.UnspecifiedWork,
            summary.IndirectCost,
            summary.TaxableIncome,
            summary.ConstructionCostBeforeTax,
            summary.VAT,
            summary.TotalConstructionCost,
            summary.OverheadRate,
            summary.TemporaryHouseRate,
            summary.UnspecifiedWorkRate,
            summary.TaxableIncomeRate,
            summary.VATRate);

    public ProfessionalBidScenario ApplyBidScenario(
        ProjectCostSummary summary,
        decimal profitRate,
        decimal riskRate,
        decimal discountRate)
    {
        RequireRate(profitRate, nameof(profitRate));
        RequireRate(riskRate, nameof(riskRate));
        RequireRate(discountRate, nameof(discountRate));

        var baseValue = summary.ConstructionCostBeforeTax;
        var profit = baseValue * profitRate;
        var risk = baseValue * riskRate;
        var preDiscount = baseValue + profit + risk;
        var discount = preDiscount * discountRate;
        var bidBeforeVat = preDiscount - discount;
        var vat = summary.VATRate > 0 ? bidBeforeVat * summary.VATRate : 0m;

        return new ProfessionalBidScenario(
            baseValue, profitRate, profit, riskRate, risk, discountRate, discount,
            bidBeforeVat, summary.VATRate, vat, bidBeforeVat + vat);
    }

    public ProfessionalRevisionResult Compare(IReadOnlyList<CostLine> baseline, IReadOnlyList<CostLine> revised)
    {
        var oldMap = baseline.GroupBy(x => x.WorkCode, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Quantity), StringComparer.OrdinalIgnoreCase);
        var newMap = revised.GroupBy(x => x.WorkCode, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Quantity), StringComparer.OrdinalIgnoreCase);

        var codes = oldMap.Keys.Concat(newMap.Keys).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x).ToList();
        var changes = new List<ProfessionalQuantityChange>();
        foreach (var code in codes)
        {
            oldMap.TryGetValue(code, out var oldQty);
            newMap.TryGetValue(code, out var newQty);
            if (oldQty != newQty)
                changes.Add(new(code, oldQty, newQty, newQty - oldQty));
        }
        return new ProfessionalRevisionResult(changes);
    }

    public IReadOnlyList<ProfessionalResourceRow> BuildResourceLedger(IReadOnlyList<CostLine> lines)
    {
        return lines
            .SelectMany(x => new[]
            {
                new ProfessionalResourceRow(x.WorkCode, ResourceType.Material, x.Material),
                new ProfessionalResourceRow(x.WorkCode, ResourceType.Labor, x.Labor),
                new ProfessionalResourceRow(x.WorkCode, ResourceType.Machine, x.Machine)
            })
            .Where(x => x.Amount != 0m)
            .GroupBy(x => x.ResourceType)
            .Select(g => new ProfessionalResourceRow("TOTAL", g.Key, g.Sum(x => x.Amount)))
            .ToList();
    }

    private static void RequireRate(decimal value, string name)
    {
        if (value < 0m || value > 1m)
            throw new ArgumentOutOfRangeException(name, "Tỷ lệ phải nằm trong khoảng 0..1. DTBK không tự áp tỷ lệ pháp lý chưa có căn cứ.");
    }
}

public sealed record ProfessionalIssue(string Code, string Severity, string Message);
public sealed record ProfessionalEstimateReview(IReadOnlyList<ProfessionalIssue> Issues)
{
    public bool CanRelease => Issues.All(x => !string.Equals(x.Severity, "error", StringComparison.OrdinalIgnoreCase));
    public int Errors => Issues.Count(x => x.Severity.Equals("error", StringComparison.OrdinalIgnoreCase));
    public int Warnings => Issues.Count(x => x.Severity.Equals("warning", StringComparison.OrdinalIgnoreCase));
}

public sealed record ProfessionalCostBreakdown(
    decimal Material, decimal Labor, decimal Machine, decimal Direct,
    decimal Overhead, decimal TemporaryHouse, decimal UnspecifiedWork, decimal Indirect,
    decimal TaxableIncome, decimal BeforeTax, decimal VAT, decimal Total,
    decimal OverheadRate, decimal TemporaryHouseRate, decimal UnspecifiedWorkRate,
    decimal TaxableIncomeRate, decimal VATRate);

public sealed record ProfessionalBidScenario(
    decimal BaseValue, decimal ProfitRate, decimal Profit, decimal RiskRate, decimal Risk,
    decimal DiscountRate, decimal Discount, decimal BidBeforeVat, decimal VATRate,
    decimal VAT, decimal BidTotal);

public sealed record ProfessionalQuantityChange(string WorkCode, decimal BaselineQuantity, decimal RevisedQuantity, decimal Delta);
public sealed record ProfessionalRevisionResult(IReadOnlyList<ProfessionalQuantityChange> Changes)
{
    public decimal NetQuantityDelta => Changes.Sum(x => x.Delta);
}

public sealed record ProfessionalResourceRow(string WorkCode, ResourceType ResourceType, decimal Amount);
