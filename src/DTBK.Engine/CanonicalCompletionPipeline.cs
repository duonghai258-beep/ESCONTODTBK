using DTBK.Domain;

namespace DTBK.Engine;

/// <summary>
/// Single application-level closure for C01-C12.
/// Existing domain engines remain authoritative; this type only orchestrates them.
/// No calculation, workbook or commercial engine is duplicated here.
/// </summary>
public sealed class CanonicalCompletionPipeline
{
    private readonly WorkbookControlRuntime _controls;
    private readonly CalculationStateReportSource _reports;
    private readonly CommercialWorkflowRuntime _commercial;

    public CanonicalCompletionPipeline(WorkbookControlRuntime controls, CalculationStateReportSource? reports = null, CommercialWorkflowRuntime? commercial = null)
    {
        _controls = controls ?? throw new ArgumentNullException(nameof(controls));
        _reports = reports ?? new CalculationStateReportSource();
        _commercial = commercial ?? new CommercialWorkflowRuntime();
    }

    public RuntimeCalculationResult Calculate(string sheetId,string tableId,string rowKey,string columnId,string? value,CalculationContext context,WorkType workType,CalculationScenario scenario,LocationType location,int provinceId,string vatCategory = "STANDARD")
        => new CanonicalRuntimeWorkflow(_controls,_reports,_commercial).RecalculateFromControl(sheetId,tableId,rowKey,columnId,value,context,workType,scenario,location,provinceId,vatCategory);

    public ReportRequest Report(string projectName,string investor,string contractor,string location,DateTime calculationDate,CalculationState state,string preparedBy = "",string checkedBy = "")
        => _reports.CreateRequest(projectName,investor,contractor,location,calculationDate,state,preparedBy,checkedBy);

    public CommercialWorkflowState StartCommercial(string projectId,CalculationState state,string snapshotId)
        => _commercial.StartEstimate(projectId,state,snapshotId);

    public CommercialWorkflowState AdvanceCommercial(CommercialStage stage,CalculationState state,string snapshotId)
        => _commercial.Advance(stage,state,snapshotId);
}

/// <summary>Canonical closure map. C10 covers the evidence-backed ESCON 24-sheet workspace.</summary>
public static class CanonicalCompletionStages
{
    public static IReadOnlyList<string> All { get; } = new[]
    {
        "C01 Multi-source Parser",
        "C02 BG/XML/Excel canonical ingestion",
        "C03 Norm domain",
        "C04 Price domain",
        "C05 Material/Labor/Machine/Fuel",
        "C06 Transport",
        "C07 Workbook/Cell/Provenance",
        "C08 Formula/SPVALUE/SUMGROUP",
        "C09 Dependency/Recalculation",
        "C10 24-sheet ESCON semantic projection + GUI",
        "C11 Snapshot/Legal/Audit/Report",
        "C12 Commercial workflow + final integration"
    };

    public static bool IsCanonicalOrder(IReadOnlyList<string> stages)
        => stages.Count == All.Count && stages.SequenceEqual(All, StringComparer.Ordinal);
}
