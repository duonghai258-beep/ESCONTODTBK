using DTBK.Domain;

namespace DTBK.Engine;

public sealed class WorkbookControlRuntime
{
    private readonly EstimationWorkbook _workbook;
    private readonly WorkbookCalculationComposition _calculation;
    private readonly RecalculationScheduler _scheduler;
    private readonly Canonical15SheetProjectionWriters _projection;
    public WorkbookControlRuntime(EstimationWorkbook workbook, WorkbookCalculationComposition calculation, RecalculationScheduler? scheduler = null, Canonical15SheetProjectionWriters? projection = null)
    { _workbook = workbook ?? throw new ArgumentNullException(nameof(workbook)); _calculation = calculation ?? throw new ArgumentNullException(nameof(calculation)); _scheduler = scheduler ?? new RecalculationScheduler(); _projection = projection ?? new Canonical15SheetProjectionWriters(); }
    public CalculationState ApplyInput(string sheetId, string tableId, string rowKey, string columnId, string? value, CalculationContext context, WorkType workType, CalculationScenario scenario, LocationType location, int provinceId, string vatCategory = "STANDARD")
    {
        if (string.IsNullOrWhiteSpace(sheetId)) throw new ArgumentException("SheetId is required.", nameof(sheetId)); if (string.IsNullOrWhiteSpace(tableId)) throw new ArgumentException("TableId is required.", nameof(tableId)); if (string.IsNullOrWhiteSpace(rowKey)) throw new ArgumentException("RowKey is required.", nameof(rowKey)); if (string.IsNullOrWhiteSpace(columnId)) throw new ArgumentException("ColumnId is required.", nameof(columnId));
        var sheet = _workbook.GetOrCreateSheet(sheetId, sheetId); var table = sheet.GetOrCreateTable(tableId); var cell = table.GetOrCreateCell(rowKey, columnId); cell.SetValue(value); var changedAddress = CanonicalAddress(sheetId, tableId, rowKey, columnId); CanonicalFormulaRegistry.Register(_scheduler, rowKey); var plan = _scheduler.Plan(new[] { changedAddress }); var state = _calculation.Calculate(context, workType, scenario, location, provinceId, vatCategory); _projection.WriteAll(_workbook, state); LastPlan = plan; return state;
    }
    public RecalculationPlan? LastPlan { get; private set; }
    private static string CanonicalAddress(string sheetId, string tableId, string rowKey, string columnId) => $"{sheetId}/{tableId}/{rowKey}/{columnId}";
}

public sealed class CalculationStateReportSource
{
    public ReportRequest CreateRequest(string projectName, string investor, string contractor, string location, DateTime calculationDate, CalculationState state, string preparedBy = "", string checkedBy = "")
    { ArgumentNullException.ThrowIfNull(state); return new ReportRequest(projectName, investor, contractor, location, calculationDate, state.Summary, state.Lines.Select(x => x.Work).ToList(), preparedBy, checkedBy, state); }
}

public enum CommercialStage { Estimate = 1, Bid = 2, Contract = 3, Variation = 4, Payment = 5, FinalSettlement = 6 }
public sealed record CommercialWorkflowState(string ProjectId, CommercialStage Stage, string SnapshotId, DateTime ChangedAt, string CalculationCode);
public sealed class CommercialWorkflowRuntime
{
    private CommercialWorkflowState? _current;
    public CommercialWorkflowState StartEstimate(string projectId, CalculationState state, string snapshotId) => Set(projectId, CommercialStage.Estimate, state, snapshotId);
    public CommercialWorkflowState Advance(CommercialStage nextStage, CalculationState state, string snapshotId)
    { ArgumentNullException.ThrowIfNull(state); if (_current is null) throw new InvalidOperationException("Commercial workflow has not been started."); if ((int)nextStage != (int)_current.Stage + 1) throw new InvalidOperationException($"Invalid commercial transition {_current.Stage} -> {nextStage}."); return Set(_current.ProjectId, nextStage, state, snapshotId); }
    public CommercialWorkflowState Current => _current ?? throw new InvalidOperationException("Commercial workflow has not been started.");
    private CommercialWorkflowState Set(string projectId, CommercialStage stage, CalculationState state, string snapshotId)
    { if (string.IsNullOrWhiteSpace(projectId)) throw new ArgumentException("ProjectId is required.", nameof(projectId)); if (string.IsNullOrWhiteSpace(snapshotId)) throw new ArgumentException("SnapshotId is required.", nameof(snapshotId)); ArgumentNullException.ThrowIfNull(state); return _current = new CommercialWorkflowState(projectId, stage, snapshotId, DateTime.UtcNow, state.Trace.CalculationCode); }
}

public sealed class CanonicalRuntimeWorkflow
{
    private readonly WorkbookControlRuntime _controls; private readonly CalculationStateReportSource _reports; private readonly CommercialWorkflowRuntime _commercial;
    public CanonicalRuntimeWorkflow(WorkbookControlRuntime controls, CalculationStateReportSource? reports = null, CommercialWorkflowRuntime? commercial = null)
    { _controls = controls ?? throw new ArgumentNullException(nameof(controls)); _reports = reports ?? new CalculationStateReportSource(); _commercial = commercial ?? new CommercialWorkflowRuntime(); }
    public RuntimeCalculationResult RecalculateFromControl(string sheetId, string tableId, string rowKey, string columnId, string? value, CalculationContext context, WorkType workType, CalculationScenario scenario, LocationType location, int provinceId, string vatCategory = "STANDARD")
    { var state = _controls.ApplyInput(sheetId, tableId, rowKey, columnId, value, context, workType, scenario, location, provinceId, vatCategory); return new RuntimeCalculationResult(state, state.Resources, state.Lines, state.Summary, state.Trace.CalculationCode, _controls.LastPlan); }
    public ReportRequest BuildReport(string projectName, string investor, string contractor, string location, DateTime calculationDate, CalculationState state, string preparedBy = "", string checkedBy = "") => _reports.CreateRequest(projectName, investor, contractor, location, calculationDate, state, preparedBy, checkedBy);
    public CommercialWorkflowState StartCommercial(string projectId, CalculationState state, string snapshotId) => _commercial.StartEstimate(projectId, state, snapshotId);
    public CommercialWorkflowState AdvanceCommercial(CommercialStage stage, CalculationState state, string snapshotId) => _commercial.Advance(stage, state, snapshotId);
}

public sealed record RuntimeCalculationResult(CalculationState State, IReadOnlyList<ResourceConsumption> Resources, IReadOnlyList<CalculationLineState> Lines, ProjectCostSummary Summary, string CalculationCode, RecalculationPlan? RecalculationPlan);
