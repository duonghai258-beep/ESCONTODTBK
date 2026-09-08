using DTBK.Domain;

namespace DTBK.Engine;

/// <summary>
/// Deterministic geometry-to-quantity kernel. UI/import adapters are intentionally outside this class.
/// Coordinates are in drawing units and calibration converts them to the declared real-world unit.
/// </summary>
public sealed class QuantityTakeoffEngine
{
    public decimal MeasureLength(IReadOnlyList<(decimal X, decimal Y)> points, DrawingCalibration calibration)
    {
        Validate(points, calibration, GeometryType.Line);
        decimal drawingLength = 0m;
        for (var i = 1; i < points.Count; i++)
            drawingLength += Distance(points[i - 1], points[i]);
        return drawingLength * calibration.ScaleFactor;
    }

    public decimal MeasureArea(IReadOnlyList<(decimal X, decimal Y)> points, DrawingCalibration calibration)
    {
        Validate(points, calibration, GeometryType.Polygon);
        decimal twiceArea = 0m;
        for (var i = 0; i < points.Count; i++)
        {
            var a = points[i];
            var b = points[(i + 1) % points.Count];
            twiceArea += (a.X * b.Y) - (b.X * a.Y);
        }
        var scale = calibration.ScaleFactor;
        return Math.Abs(twiceArea) / 2m * scale * scale;
    }

    public decimal MeasureCount(int count)
    {
        if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
        return count;
    }

    public decimal MeasureVolume(decimal area, decimal depth)
    {
        if (area < 0) throw new ArgumentOutOfRangeException(nameof(area));
        if (depth < 0) throw new ArgumentOutOfRangeException(nameof(depth));
        return area * depth;
    }

    public QuantityLink LinkToEstimate(Measurement measurement, long projectItemId)
    {
        ArgumentNullException.ThrowIfNull(measurement);
        if (!measurement.IsAuthoritative) throw new InvalidOperationException("Non-authoritative measurements cannot drive an estimate.");
        if (string.IsNullOrWhiteSpace(measurement.WorkCode)) throw new InvalidOperationException("Measurement requires WorkCode.");
        if (measurement.Quantity < 0) throw new InvalidOperationException("Measurement quantity cannot be negative.");
        return new(measurement.MeasurementId, projectItemId, measurement.WorkCode, measurement.NormCode,
            measurement.Quantity, measurement.Unit, measurement.SourceHash, measurement.RevisionId);
    }

    private static decimal Distance((decimal X, decimal Y) a, (decimal X, decimal Y) b)
    {
        var dx = b.X - a.X;
        var dy = b.Y - a.Y;
        var squared = checked(dx * dx + dy * dy);
        return DecimalSqrt(squared);
    }

    private static decimal DecimalSqrt(decimal value)
    {
        if (value < 0m) throw new ArgumentOutOfRangeException(nameof(value));
        if (value == 0m) return 0m;

        // Newton-Raphson using decimal arithmetic only. This avoids converting
        // geometry coordinates to double and losing deterministic quantity precision.
        var estimate = value >= 1m ? value / 2m : 1m;
        for (var i = 0; i < 128; i++)
        {
            var next = (estimate + value / estimate) / 2m;
            if (next == estimate) return next;
            estimate = next;
        }
        return estimate;
    }

    private static void Validate(IReadOnlyList<(decimal X, decimal Y)> points, DrawingCalibration calibration, GeometryType type)
    {
        ArgumentNullException.ThrowIfNull(points);
        ArgumentNullException.ThrowIfNull(calibration);
        var minimum = type == GeometryType.Polygon ? 3 : 2;
        if (points.Count < minimum) throw new ArgumentException($"{type} requires at least {minimum} points.", nameof(points));
    }
}

public sealed class TakeoffRevisionEngine
{
    public IReadOnlyList<MeasurementRevisionDelta> Compare(
        IEnumerable<Measurement> baseline,
        IEnumerable<Measurement> revised)
    {
        var oldMap = baseline.ToDictionary(Key, StringComparer.Ordinal);
        var newMap = revised.ToDictionary(Key, StringComparer.Ordinal);
        var keys = oldMap.Keys.Union(newMap.Keys, StringComparer.Ordinal).OrderBy(x => x, StringComparer.Ordinal);
        var result = new List<MeasurementRevisionDelta>();
        foreach (var key in keys)
        {
            oldMap.TryGetValue(key, out var oldItem);
            newMap.TryGetValue(key, out var newItem);
            var oldQty = oldItem?.Quantity ?? 0m;
            var newQty = newItem?.Quantity ?? 0m;
            var type = oldItem is null ? RevisionChangeType.Added : newItem is null ? RevisionChangeType.Removed :
                oldItem.SourceHash == newItem.SourceHash && oldQty == newQty ? RevisionChangeType.Unchanged : RevisionChangeType.Modified;
            result.Add(new(key, type, oldQty, newQty, newQty - oldQty, oldItem?.SourceHash ?? "", newItem?.SourceHash ?? ""));
        }
        return result;
    }

    private static string Key(Measurement m) => $"{m.DimensionGroupId}|{m.WorkCode}|{m.NormCode}|{m.Unit}";
}
