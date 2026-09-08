using DTBK.Domain;
using System.Security.Cryptography;
using System.Text;

namespace DTBK.Engine;

/// <summary>
/// Fail-closed ingestion boundary for PDF/CAD/BIM/manual measurement adapters.
/// Adapters may extract facts, but this service is the only boundary allowed to turn
/// those facts into authoritative DTBK measurements.
/// </summary>
public sealed class AuthoritativeTakeoffImportService
{
    public IReadOnlyList<Measurement> Import(DrawingDocument document, DrawingRevision revision, IEnumerable<ImportedMeasurementFact> facts)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(revision);
        ArgumentNullException.ThrowIfNull(facts);

        if (!string.Equals(document.SourceHash, revision.ContentHash, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Drawing source hash and revision content hash do not match.");
        if (!string.Equals(revision.DrawingId, document.DrawingId, StringComparison.Ordinal))
            throw new InvalidOperationException("Drawing revision belongs to another drawing.");

        var result = new List<Measurement>();
        foreach (var fact in facts)
        {
            ValidateFact(fact);
            var geometryHash = string.IsNullOrWhiteSpace(fact.Geometry.GeometryHash) ? HashGeometry(fact.Geometry) : fact.Geometry.GeometryHash;
            var geometry = fact.Geometry with { GeometryHash = geometryHash };
            var evidenceHash = string.IsNullOrWhiteSpace(fact.EvidenceHash) ? HashEvidence(document, revision, fact, geometryHash) : fact.EvidenceHash;
            result.Add(new Measurement(fact.MeasurementId, revision.RevisionId, fact.Type, geometry.Type, fact.Quantity,
                fact.Unit, fact.WorkCode, fact.NormCode, fact.DimensionGroupId, geometry,
                document.SourceHash, evidenceHash, fact.Formula, fact.IsAuthoritative));
        }
        return result;
    }

    private static void ValidateFact(ImportedMeasurementFact fact)
    {
        if (string.IsNullOrWhiteSpace(fact.MeasurementId)) throw new InvalidOperationException("MeasurementId is required.");
        if (string.IsNullOrWhiteSpace(fact.WorkCode)) throw new InvalidOperationException($"Measurement {fact.MeasurementId} requires WorkCode.");
        if (string.IsNullOrWhiteSpace(fact.NormCode)) throw new InvalidOperationException($"Measurement {fact.MeasurementId} requires NormCode.");
        if (string.IsNullOrWhiteSpace(fact.Unit)) throw new InvalidOperationException($"Measurement {fact.MeasurementId} requires Unit.");
        if (string.IsNullOrWhiteSpace(fact.DimensionGroupId)) throw new InvalidOperationException($"Measurement {fact.MeasurementId} requires DimensionGroupId.");
        if (fact.Geometry.Type == GeometryType.None && fact.Type != MeasurementType.Count)
            throw new InvalidOperationException($"Measurement {fact.MeasurementId} requires geometry unless it is a count.");
        if (fact.Geometry.Type != GeometryType.None && fact.Geometry.Points.Count == 0 && fact.Type != MeasurementType.Count)
            throw new InvalidOperationException($"Measurement {fact.MeasurementId} has empty geometry.");
        if (fact.Quantity < 0 && string.IsNullOrWhiteSpace(fact.EvidenceHash))
            throw new InvalidOperationException($"Measurement {fact.MeasurementId} has negative quantity without evidence.");
    }

    private static string HashGeometry(MeasurementGeometry geometry)
    {
        var canonical = geometry.Type + "|" + geometry.CoordinateSystem + "|" + string.Join(";", geometry.Points.Select(p => $"{p.X:R},{p.Y:R}"));
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
    }

    private static string HashEvidence(DrawingDocument document, DrawingRevision revision, ImportedMeasurementFact fact, string geometryHash)
    {
        var canonical = string.Join("|", document.DrawingId, document.SourceHash, revision.RevisionId, revision.ContentHash,
            fact.MeasurementId, fact.Type, fact.Quantity.ToString("R"), fact.Unit, fact.WorkCode, fact.NormCode,
            fact.DimensionGroupId, fact.Formula, geometryHash);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
    }
}

public sealed record ImportedMeasurementFact(
    string MeasurementId,
    MeasurementType Type,
    decimal Quantity,
    string Unit,
    string WorkCode,
    string NormCode,
    string DimensionGroupId,
    MeasurementGeometry Geometry,
    string Formula = "",
    string EvidenceHash = "",
    bool IsAuthoritative = true);
