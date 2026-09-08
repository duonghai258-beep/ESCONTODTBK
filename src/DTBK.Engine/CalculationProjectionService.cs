using DTBK.Domain;

namespace DTBK.Engine;

/// <summary>
/// T01-T08 are read-only projections/reconciliation over CalculationState.
/// No projection performs an independent legal or pricing calculation.
/// </summary>
public sealed class CalculationProjectionService
{
    public CalculationProjection Material(CalculationState state) => Project(state, "T01_MATERIAL", state.MaterialTotal);
    public CalculationProjection Labor(CalculationState state) => Project(state, "T02_LABOR", state.LaborTotal);
    public CalculationProjection Machine(CalculationState state) => Project(state, "T03_MACHINE", state.MachineTotal);
    public CalculationProjection Transport(CalculationState state) => Project(state, "T04_TRANSPORT", state.TransportTotal);
    public CalculationProjection UnitPrice(CalculationState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        var totalQuantity = state.Lines.Sum(x => x.Work.Quantity);
        var value = totalQuantity == 0m ? 0m : state.Lines.Sum(x => x.DirectAmount) / totalQuantity;
        return Project(state, "T05_UNIT_PRICE", value);
    }
    public CalculationProjection Cost(CalculationState state) => Project(state, "T06_COST", state.Summary.DirectCost);
    public CalculationProjection Estimate(CalculationState state) => Project(state, "T07_ESTIMATE", state.Summary.TotalConstructionCost);
    public ReconciliationResult Reconcile(CalculationState state, string projectionCode, decimal expectedValue, decimal reportedValue, decimal numericTolerance)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (numericTolerance < 0m) throw new ArgumentOutOfRangeException(nameof(numericTolerance));
        var difference = reportedValue - expectedValue;
        var status = Math.Abs(difference) <= numericTolerance ? state.VerificationStatus : DTBK.Domain.VerificationStatus.NeedsVerification;
        return new ReconciliationResult(projectionCode, expectedValue, reportedValue, difference, numericTolerance, status, state.Trace.CalculationCode, state.Trace);
    }
    public ReconciliationResult Reconcile(CalculationState state, string projectionCode, decimal expectedValue, decimal reportedValue, CalculationTolerance tolerance)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(tolerance);
        if (tolerance.VerificationStatus == DTBK.Domain.VerificationStatus.Blocked)
            return new ReconciliationResult(projectionCode, expectedValue, reportedValue, reportedValue - expectedValue, tolerance.MaximumAbsoluteDifference, DTBK.Domain.VerificationStatus.Blocked, state.Trace.CalculationCode, state.Trace);
        return Reconcile(state, projectionCode, expectedValue, reportedValue, tolerance.MaximumAbsoluteDifference);
    }
    private static CalculationProjection Project(CalculationState state, string code, decimal value)
    {
        ArgumentNullException.ThrowIfNull(state);
        return new(code, value, state.VerificationStatus, state.Trace.CalculationCode, state.Trace);
    }
}
