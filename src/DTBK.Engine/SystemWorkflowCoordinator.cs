using System.Security.Cryptography;
using System.Text;
using DTBK.Domain;

namespace DTBK.Engine;

public sealed class SystemWorkflowCoordinator
{
    private readonly CalculationStateEngine _calculation;
    private readonly CalculationProjectionService _projection;
    private readonly AuthoritativeTakeoffImportService _takeoff;
    private readonly ProjectLifecycleEngine _lifecycle;

    public SystemWorkflowCoordinator(CalculationStateEngine calculation, CalculationProjectionService? projection = null, AuthoritativeTakeoffImportService? takeoff = null, ProjectLifecycleEngine? lifecycle = null)
    {
        _calculation = calculation ?? throw new ArgumentNullException(nameof(calculation));
        _projection = projection ?? new CalculationProjectionService();
        _takeoff = takeoff ?? new AuthoritativeTakeoffImportService();
        _lifecycle = lifecycle ?? new ProjectLifecycleEngine();
    }
    public IReadOnlyList<Measurement> ImportTakeoff(DrawingDocument document, DrawingRevision revision, IEnumerable<ImportedMeasurementFact> facts) => _takeoff.Import(document, revision, facts);
    public IReadOnlyList<ProjectItem> ApplyTakeoff(IReadOnlyList<ProjectItem> items, IReadOnlyList<Measurement> measurements)
    {
        ArgumentNullException.ThrowIfNull(items); ArgumentNullException.ThrowIfNull(measurements);
        var byWork = measurements.GroupBy(x => x.WorkCode, StringComparer.OrdinalIgnoreCase).ToDictionary(g => g.Key, g => g.Sum(x => x.Quantity), StringComparer.OrdinalIgnoreCase);
        return items.Select(item => byWork.TryGetValue(item.WorkCode, out var quantity) ? item with { Quantity = quantity } : item).ToList();
    }
    public CalculationState Calculate(CalculationRequest request) => _calculation.Calculate(request);
    public ValidationResult Validate(CalculationState state)
    {
        ArgumentNullException.ThrowIfNull(state); var issues = new List<ValidationIssue>();
        if (state.Lines.Count == 0) issues.Add(new ValidationIssue(ValidationSeverity.Error, "CALC_EMPTY", "Không có dòng tính toán."));
        foreach (var line in state.Lines)
        {
            if (line.Work.Quantity < 0) issues.Add(new ValidationIssue(ValidationSeverity.Error, "QTY_NEGATIVE", "Khối lượng âm.", line.Work.ProjectItemID));
            if (string.IsNullOrWhiteSpace(line.Work.NormCode)) issues.Add(new ValidationIssue(ValidationSeverity.Error, "NORM_MISSING", "Thiếu mã định mức.", line.Work.ProjectItemID));
            if (line.Provenance.VerificationStatus == DTBK.Domain.VerificationStatus.Blocked) issues.Add(new ValidationIssue(ValidationSeverity.Error, "PROVENANCE_BLOCKED", "Nguồn tính bị chặn.", line.Work.ProjectItemID));
            else if (line.Provenance.VerificationStatus != DTBK.Domain.VerificationStatus.Verified) issues.Add(new ValidationIssue(ValidationSeverity.Warning, "PROVENANCE_UNVERIFIED", "Nguồn tính chưa được xác minh.", line.Work.ProjectItemID));
        }
        var componentTotal = state.MaterialTotal + state.LaborTotal + state.MachineTotal + state.TransportTotal + state.OtherDirectTotal;
        if (componentTotal != state.Summary.DirectCost) issues.Add(new ValidationIssue(ValidationSeverity.Error, "DIRECT_RECONCILIATION", "Tổng thành phần trực tiếp không khớp DirectCost."));
        return new ValidationResult(issues);
    }
    public IReadOnlyList<CalculationProjection> ProjectAll(CalculationState state) { ArgumentNullException.ThrowIfNull(state); return new[] { _projection.Material(state), _projection.Labor(state), _projection.Machine(state), _projection.Transport(state), _projection.UnitPrice(state), _projection.Cost(state), _projection.Estimate(state) }; }
    public CalculationExplanation Explain(CalculationState state, string resultCode, decimal value)
    {
        ArgumentNullException.ThrowIfNull(state); var inputs = new List<string> { $"Project={state.Context.ProjectId}", $"DataPack={state.Context.DataPackVersion}", $"Legal={state.Context.LegalRuleVersion}", $"Province={state.Context.ProvinceCode}", $"AsOfDate={state.Context.AsOfDate:yyyy-MM-dd}" };
        var rules = state.Summary.RuleProvenance.Select(x => $"{x.RuleId}:{x.FormulaMetadata}").Distinct(StringComparer.Ordinal).ToList(); var legal = state.Summary.RuleProvenance.Select(x => x.SourceId).Distinct(StringComparer.Ordinal).ToList(); var prices = state.Resources.Where(x => x.UnitPrice.HasValue).Select(x => x.ResourceCode).Distinct(StringComparer.Ordinal).ToList();
        return new CalculationExplanation(resultCode, value, string.Join("; ", state.Trace.Steps.Select(x => x.Key + "=" + x.Value)), inputs, rules, legal, prices, state.Lines.FirstOrDefault()?.Provenance.RoundingRule ?? "Engine default", $"Canonical CalculationState trace={state.Trace.CalculationCode}; inputHash={state.Trace.InputHash}; outputHash={state.Trace.OutputHash}");
    }
    public ProjectVariation RegisterVariation(ProjectVariation variation) => _lifecycle.CalculateVariation(variation);
    public PaymentPeriod RegisterPayment(long projectId, string periodCode, decimal acceptedValue, decimal currentValue, decimal cumulativeValue, decimal paidValue, DateTime effectiveDate, string sourceReference, string sourceHash) => _lifecycle.CalculatePayment(projectId, periodCode, acceptedValue, currentValue, cumulativeValue, paidValue, effectiveDate, sourceReference, sourceHash);
    public ProgressPeriod RegisterProgress(long projectId, string workCode, string periodCode, decimal plannedQuantity, decimal actualQuantity, decimal value, DateTime effectiveDate, string sourceReference, string sourceHash) => _lifecycle.CalculateProgress(projectId, workCode, periodCode, plannedQuantity, actualQuantity, value, effectiveDate, sourceReference, sourceHash);
    public QuoteComparison CompareQuotes(string tenderId, IReadOnlyList<SubcontractorQuote> quotes, IReadOnlyList<ProjectItem> items)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenderId); ArgumentNullException.ThrowIfNull(quotes); ArgumentNullException.ThrowIfNull(items); var lines = new List<QuoteComparisonLine>(); var totals = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in items) { var rates = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase); foreach (var quote in quotes.Where(q => q.Status is QuoteStatus.Submitted or QuoteStatus.Accepted)) { var match = quote.Lines.FirstOrDefault(x => string.Equals(x.WorkCode, item.WorkCode, StringComparison.OrdinalIgnoreCase)); if (match is null) continue; rates[quote.SubcontractorId] = match.UnitRate; totals.TryGetValue(quote.SubcontractorId, out var total); totals[quote.SubcontractorId] = total + match.ResolvedAmount; } var lowest = rates.Count == 0 ? 0m : rates.Values.Min(); var recommended = rates.FirstOrDefault(x => x.Value == lowest).Key; lines.Add(new QuoteComparisonLine(item.WorkCode, item.Unit, item.Quantity, rates, string.IsNullOrWhiteSpace(recommended) ? null : recommended, lowest)); }
        var evidence = Hash(string.Join("|", quotes.Select(q => q.QuoteId + ":" + q.SourceHash).OrderBy(x => x))); return new QuoteComparison(tenderId, lines, totals, evidence);
    }
    public ReportRequest CreateReportRequest(CalculationState state, string preparedBy = "", string checkedBy = "") { ArgumentNullException.ThrowIfNull(state); return new ReportRequest(state.Context.ProjectName, state.Context.Investor, "", state.Context.LocationCode, state.Context.AsOfDate, state.Summary, state.Lines.Select(x => x.Work).ToList(), preparedBy, checkedBy, state); }
    public IReadOnlySet<string> Invalidate(params string[] changedNodes) => CalculationDependencyGraph.Invalidate(changedNodes);
    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}
