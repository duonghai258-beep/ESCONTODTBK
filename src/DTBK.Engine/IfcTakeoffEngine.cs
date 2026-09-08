using System.Globalization;
using System.Text.RegularExpressions;
using DTBK.Domain;

namespace DTBK.Engine;

/// <summary>
/// Minimal, fail-closed IFC STEP quantity reader. It extracts explicit IFC quantity entities
/// and never invents geometry or quantities when the model does not expose them.
/// </summary>
public sealed class IfcTakeoffEngine
{
    private static readonly Regex EntityRegex = new("#(?<id>\\d+)\\s*=\\s*(?<type>IFC[A-Z0-9_]+)\\((?<args>.*?)\\);", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);

    public IReadOnlyList<IfcQuantity> ExtractExplicitQuantities(string ifcText)
    {
        if (string.IsNullOrWhiteSpace(ifcText)) throw new ArgumentException("IFC content is empty.", nameof(ifcText));
        var result = new List<IfcQuantity>();
        foreach (Match match in EntityRegex.Matches(ifcText))
        {
            var type = match.Groups["type"].Value.ToUpperInvariant();
            var args = match.Groups["args"].Value;
            var kind = type switch
            {
                "IFCQUANTITYLENGTH" => MeasurementType.Length,
                "IFCQUANTITYAREA" => MeasurementType.Area,
                "IFCQUANTITYVOLUME" => MeasurementType.Volume,
                "IFCQUANTITYCOUNT" => MeasurementType.Count,
                _ => (MeasurementType?)null
            };
            if (kind is null) continue;
            var tokens = SplitArguments(args);
            if (tokens.Count < 4) continue;
            var name = Unquote(tokens[0]);
            var valueToken = tokens[^1].Trim();
            if (!decimal.TryParse(valueToken, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) || value < 0) continue;
            result.Add(new IfcQuantity(match.Groups["id"].Value, kind.Value, name, value));
        }
        return result;
    }

    public IReadOnlyList<Measurement> ToMeasurements(string revisionId, IEnumerable<IfcQuantity> quantities,
        string workCode, string normCode, string unit, string sourceHash)
    {
        if (string.IsNullOrWhiteSpace(revisionId)) throw new ArgumentException("RevisionId is required.", nameof(revisionId));
        if (string.IsNullOrWhiteSpace(workCode)) throw new ArgumentException("WorkCode is required.", nameof(workCode));
        if (string.IsNullOrWhiteSpace(normCode)) throw new ArgumentException("NormCode is required.", nameof(normCode));
        if (string.IsNullOrWhiteSpace(unit)) throw new ArgumentException("Unit is required.", nameof(unit));
        if (string.IsNullOrWhiteSpace(sourceHash)) throw new ArgumentException("SourceHash is required.", nameof(sourceHash));
        ArgumentNullException.ThrowIfNull(quantities);

        return quantities.Select((q, i) => new Measurement(
            $"ifc:{q.EntityId}:{i}", revisionId, q.MeasurementType, GeometryType.BimElement,
            q.Value, unit, workCode, normCode, $"IFC:{q.Name}",
            new MeasurementGeometry(GeometryType.BimElement, Array.Empty<(decimal X, decimal Y)>()),
            sourceHash, sourceHash, $"IFC explicit quantity {q.Name}", true)).ToList();
    }

    private static List<string> SplitArguments(string text)
    {
        var result = new List<string>(); var start = 0; var depth = 0; var quoted = false;
        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            if (c == '\'') quoted = !quoted;
            else if (!quoted && c == '(') depth++;
            else if (!quoted && c == ')') depth--;
            else if (!quoted && depth == 0 && c == ',') { result.Add(text[start..i].Trim()); start = i + 1; }
        }
        result.Add(text[start..].Trim());
        return result;
    }

    private static string Unquote(string value) => value.Trim().Trim('\'');
}

public sealed record IfcQuantity(string EntityId, MeasurementType MeasurementType, string Name, decimal Value);
