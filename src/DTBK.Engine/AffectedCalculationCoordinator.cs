namespace DTBK.Engine;

/// <summary>
/// Canonical orchestration boundary for affected-only recalculation.
/// The scheduler determines what is invalidated; the supplied canonical calculation
/// delegate performs the actual estimation calculation. No GUI calculation is allowed here.
/// </summary>
public sealed class AffectedCalculationCoordinator
{
    private readonly RecalculationScheduler _scheduler;

    public AffectedCalculationCoordinator(RecalculationScheduler scheduler)
    {
        _scheduler = scheduler ?? throw new ArgumentNullException(nameof(scheduler));
    }

    public RecalculationExecution Execute(
        IEnumerable<string> changedAddresses,
        Func<IReadOnlyList<string>, object?> canonicalCalculation)
    {
        ArgumentNullException.ThrowIfNull(changedAddresses);
        ArgumentNullException.ThrowIfNull(canonicalCalculation);

        var plan = _scheduler.Plan(changedAddresses);
        var result = canonicalCalculation(plan.EvaluationOrder);
        return new RecalculationExecution(plan, result);
    }
}

public sealed record RecalculationExecution(
    RecalculationPlan Plan,
    object? CalculationResult);
