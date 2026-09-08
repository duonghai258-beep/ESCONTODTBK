using DTBK.Domain;

namespace DTBK.Engine;

public sealed class ProjectLifecycleEngine
{
    public ProjectVariation CalculateVariation(ProjectVariation variation)
    {
        if (variation.ProjectId <= 0) throw new ArgumentOutOfRangeException(nameof(variation.ProjectId));
        if (string.IsNullOrWhiteSpace(variation.WorkCode)) throw new ArgumentException("WorkCode is required.", nameof(variation));
        return variation with { };
    }

    public PaymentPeriod CalculatePayment(long projectId, string periodCode, decimal acceptedValue, decimal currentValue, decimal cumulativeValue, decimal paidValue, DateTime effectiveDate, string sourceReference, string sourceHash)
    {
        if (projectId <= 0) throw new ArgumentOutOfRangeException(nameof(projectId));
        if (string.IsNullOrWhiteSpace(periodCode)) throw new ArgumentException("PeriodCode is required.", nameof(periodCode));
        if (acceptedValue < 0 || currentValue < 0 || cumulativeValue < 0 || paidValue < 0) throw new ArgumentOutOfRangeException(nameof(currentValue));
        if (cumulativeValue < currentValue) throw new InvalidOperationException("Cumulative payment value cannot be below current-period value.");
        if (paidValue > cumulativeValue) throw new InvalidOperationException("Paid value cannot exceed cumulative value.");
        return new(projectId, periodCode, acceptedValue, currentValue, cumulativeValue, paidValue, effectiveDate, sourceReference, sourceHash);
    }

    public ProgressPeriod CalculateProgress(long projectId, string workCode, string periodCode, decimal plannedQuantity, decimal actualQuantity, decimal value, DateTime effectiveDate, string sourceReference, string sourceHash)
    {
        if (projectId <= 0) throw new ArgumentOutOfRangeException(nameof(projectId));
        if (string.IsNullOrWhiteSpace(workCode) || string.IsNullOrWhiteSpace(periodCode)) throw new ArgumentException("WorkCode and PeriodCode are required.");
        if (plannedQuantity < 0 || actualQuantity < 0 || value < 0) throw new ArgumentOutOfRangeException(nameof(actualQuantity));
        if (actualQuantity > plannedQuantity) throw new InvalidOperationException("Actual quantity cannot exceed planned quantity without an approved variation.");
        return new(projectId, workCode, periodCode, plannedQuantity, actualQuantity, value, effectiveDate, sourceReference, sourceHash);
    }
}
