using DTBK.Domain;

namespace DTBK.Engine;

/// <summary>
/// Canonical DTBK calculation boundary for the shared position contract.
/// This ports the deterministic quantity × unit-rate business rule into the
/// owning Engine module; Import remains responsible only for parsing/adaptation.
/// </summary>
public sealed class DTBKPositionCalculationEngine
{
    public decimal CalculateExtendedAmount(DTBKPositionContract position)
    {
        ArgumentNullException.ThrowIfNull(position);
        if (position.Quantity < 0m) throw new ArgumentOutOfRangeException(nameof(position), "Quantity cannot be negative.");
        if (position.UnitRate < 0m) throw new ArgumentOutOfRangeException(nameof(position), "UnitRate cannot be negative.");
        return checked(position.Quantity * position.UnitRate);
    }

    public IReadOnlyList<decimal> CalculateExtendedAmounts(DTBKPositionBatch batch)
    {
        ArgumentNullException.ThrowIfNull(batch);
        return batch.Positions.Select(CalculateExtendedAmount).ToArray();
    }
}
