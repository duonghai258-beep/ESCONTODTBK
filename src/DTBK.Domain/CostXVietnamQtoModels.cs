namespace DTBK.Domain;

/// <summary>
/// CostX-style dimension row: one auditable measurement/formula feeding a canonical DTBK quantity.
/// This is a worksheet contract only; costing remains in the existing DTBK cost pipeline.
/// </summary>
public sealed record QtoDimension(
    string DimensionId,
    string RevisionId,
    string Description,
    string WorkCode,
    string NormCode,
    string Unit,
    MeasurementType MeasurementType,
    string Expression,
    decimal Quantity,
    string SourceHash,
    string EvidenceHash,
    string? GeometryHash = null,
    bool IsDeduction = false,
    int Sequence = 0,
    string? ColumnKey = null,
    string? CarryDownSourceDimensionId = null)
{
    /// <summary>Stable worksheet column identity. Existing callers default to DimensionId.</summary>
    public string EffectiveColumnKey => string.IsNullOrWhiteSpace(ColumnKey) ? DimensionId : ColumnKey;

    /// <summary>Preserves the exact user-entered expression; a single '=' is the carry-down operator.</summary>
    public bool IsCarryDown => string.Equals(Expression, "=", StringComparison.Ordinal);
}

public sealed record QtoWorksheet(
    string WorksheetId,
    string RevisionId,
    string Name,
    IReadOnlyList<QtoDimension> Dimensions,
    string SourceHash,
    string EvidenceHash);

public sealed record ResolvedQtoDimension(
    QtoDimension Dimension,
    decimal Quantity,
    string EvaluatedExpression,
    string? CarryDownSourceDimensionId = null);

public sealed record QtoEstimateItem(
    long ProjectItemId,
    string WorkCode,
    string Description,
    string Unit,
    decimal Quantity,
    string NormCode,
    IReadOnlyList<string> DimensionIds);

public sealed record QtoEstimateLinkResult(
    IReadOnlyList<QtoEstimateItem> Items,
    IReadOnlyList<CostLine> CostLines,
    string WorksheetId,
    string RevisionId,
    string EvidenceHash);