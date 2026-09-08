namespace DTBK.Domain;
public enum CommercialWorkflowStage { Estimate, Bid, Contract, Variation, Payment, FinalSettlement }
public sealed record CommercialWorkflowSnapshot(string SnapshotId, CommercialWorkflowStage Stage, DateTimeOffset CreatedAt, string CalculationTraceId, string? ParentSnapshotId, string? Note);
public sealed class CommercialWorkflowState
{
    private readonly List<CommercialWorkflowSnapshot> _history = new();
    public IReadOnlyList<CommercialWorkflowSnapshot> History => _history;
    public CommercialWorkflowSnapshot Transition(CommercialWorkflowStage stage, string calculationTraceId, string? note = null)
    {
        if (string.IsNullOrWhiteSpace(calculationTraceId)) throw new ArgumentException("Calculation trace id is required.", nameof(calculationTraceId));
        var previous = _history.LastOrDefault();
        if (previous is not null && stage < previous.Stage) throw new InvalidOperationException($"Cannot move workflow backward from {previous.Stage} to {stage}.");
        var snapshot = new CommercialWorkflowSnapshot(Guid.NewGuid().ToString("N"), stage, DateTimeOffset.UtcNow, calculationTraceId, previous?.SnapshotId, note);
        _history.Add(snapshot); return snapshot;
    }
}
