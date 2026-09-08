namespace DTBK.Domain;

/// <summary>Authoritative 2D/3D takeoff source.</summary>
public enum TakeoffSourceType { Pdf = 1, Cad = 2, Ifc = 3, Revit = 4, SketchUp = 5, Manual = 9 }
public enum MeasurementType { Length = 1, Area = 2, Count = 3, Volume = 4 }
public enum GeometryType { None = 0, Line = 1, Polyline = 2, Polygon = 3, Point = 4, BimElement = 5 }
public enum RevisionChangeType { Added = 1, Removed = 2, Modified = 3, Unchanged = 4 }
public enum QuoteStatus { Draft = 1, Submitted = 2, Clarification = 3, Accepted = 4, Rejected = 5 }

public sealed record DrawingDocument(
    string DrawingId,
    string ProjectId,
    string FileName,
    TakeoffSourceType SourceType,
    string SourceHash,
    string UnitSystem = "metric",
    string? Description = null);

public sealed record DrawingRevision(
    string RevisionId,
    string DrawingId,
    string RevisionCode,
    DateTime PublishedAtUtc,
    string ContentHash,
    string? SupersedesRevisionId = null);

public sealed record DrawingCalibration(
    string CalibrationId,
    string RevisionId,
    decimal DrawingDistance,
    decimal RealDistance,
    string Unit,
    decimal ScaleFactor,
    string EvidenceHash)
{
    private static readonly HashSet<string> SupportedUnits = new(StringComparer.OrdinalIgnoreCase)
    {
        "mm", "cm", "m", "km"
    };

    public static DrawingCalibration Create(string calibrationId, string revisionId, decimal drawingDistance, decimal realDistance, string unit, string evidenceHash)
    {
        if (string.IsNullOrWhiteSpace(calibrationId)) throw new ArgumentException("CalibrationId is required.", nameof(calibrationId));
        if (string.IsNullOrWhiteSpace(revisionId)) throw new ArgumentException("RevisionId is required.", nameof(revisionId));
        if (drawingDistance <= 0) throw new ArgumentOutOfRangeException(nameof(drawingDistance), "Drawing distance must be positive.");
        if (realDistance <= 0) throw new ArgumentOutOfRangeException(nameof(realDistance), "Real distance must be positive.");
        if (string.IsNullOrWhiteSpace(unit) || !SupportedUnits.Contains(unit.Trim()))
            throw new ArgumentException("Unsupported calibration unit. Supported units: mm, cm, m, km.", nameof(unit));
        if (string.IsNullOrWhiteSpace(evidenceHash))
            throw new ArgumentException("Calibration evidence hash is required.", nameof(evidenceHash));

        return new(calibrationId, revisionId, drawingDistance, realDistance, unit.Trim().ToLowerInvariant(), realDistance / drawingDistance, evidenceHash);
    }
}

public sealed record MeasurementGeometry(
    GeometryType Type,
    IReadOnlyList<(decimal X, decimal Y)> Points,
    string CoordinateSystem = "drawing",
    string GeometryHash = "");

public sealed record Measurement(
    string MeasurementId,
    string RevisionId,
    MeasurementType Type,
    GeometryType GeometryType,
    decimal Quantity,
    string Unit,
    string WorkCode,
    string NormCode,
    string DimensionGroupId,
    MeasurementGeometry Geometry,
    string SourceHash,
    string EvidenceHash,
    string Formula = "",
    bool IsAuthoritative = true);

public sealed record DimensionGroup(
    string DimensionGroupId,
    string Name,
    string Unit,
    IReadOnlyList<string> MeasurementIds,
    decimal Multiplier = 1m,
    decimal Deduction = 0m)
{
    public decimal ResolveQuantity(IReadOnlyDictionary<string, Measurement> measurements)
    {
        ArgumentNullException.ThrowIfNull(measurements);
        var total = 0m;
        foreach (var id in MeasurementIds)
        {
            if (!measurements.TryGetValue(id, out var measurement))
                throw new InvalidOperationException($"DimensionGroup '{DimensionGroupId}' references missing measurement '{id}'.");
            total += measurement.Quantity;
        }
        return (total * Multiplier) - Deduction;
    }
}

public sealed record MeasurementRevisionDelta(
    string MeasurementKey,
    RevisionChangeType ChangeType,
    decimal OldQuantity,
    decimal NewQuantity,
    decimal DeltaQuantity,
    string OldHash,
    string NewHash);

public sealed record QuantityLink(
    string MeasurementId,
    long ProjectItemId,
    string WorkCode,
    string NormCode,
    decimal Quantity,
    string Unit,
    string MeasurementHash,
    string RevisionId);

public sealed record Subcontractor(
    string SubcontractorId,
    string Name,
    string TaxCode = "",
    string Contact = "");

public sealed record SubcontractorQuoteLine(
    string WorkCode,
    string Description,
    string Unit,
    decimal Quantity,
    decimal UnitRate,
    decimal? Amount = null)
{
    public decimal ResolvedAmount => Amount ?? Quantity * UnitRate;
}

public sealed record SubcontractorQuote(
    string QuoteId,
    string SubcontractorId,
    DateTime SubmittedAtUtc,
    QuoteStatus Status,
    IReadOnlyList<SubcontractorQuoteLine> Lines,
    string SourceHash,
    string Currency = "VND");

public sealed record QuoteComparisonLine(
    string WorkCode,
    string Unit,
    decimal Quantity,
    IReadOnlyDictionary<string, decimal> UnitRates,
    string? RecommendedSubcontractorId,
    decimal LowestUnitRate);

public sealed record QuoteComparison(
    string TenderId,
    IReadOnlyList<QuoteComparisonLine> Lines,
    IReadOnlyDictionary<string, decimal> Totals,
    string EvidenceHash);

public sealed record DataPackEntry(
    string Code,
    string Name,
    string Unit,
    string ResourceType,
    decimal Quantity,
    string SourceDocument,
    string SourcePage,
    string SourceHash,
    DateTime EffectiveFrom,
    DateTime? EffectiveTo = null);

public sealed record VietnamDataPackManifest(
    string PackageCode,
    string Version,
    DateTime EffectiveFrom,
    IReadOnlyList<string> SourceDocuments,
    string AggregateSourceHash,
    int NormCount,
    int NormDetailCount,
    int MaterialCount,
    int LaborCount,
    int EquipmentCount,
    bool ReplacementAnnexesApplied,
    bool ProductionReady);