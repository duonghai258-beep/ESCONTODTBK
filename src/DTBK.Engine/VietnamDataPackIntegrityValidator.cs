using DTBK.Domain;
using System.Security.Cryptography;
using System.Text;

namespace DTBK.Engine;

/// <summary>
/// Validates the actual row-level evidence behind a Vietnam DataPack manifest.
/// It never supplies or invents legal/rate/norm data.
/// </summary>
public sealed class VietnamDataPackIntegrityValidator
{
    private readonly VietnamDataPackValidator _manifestValidator = new();

    public DataPackIntegrityResult Validate(VietnamDataPackManifest manifest, IEnumerable<DataPackEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        ArgumentNullException.ThrowIfNull(entries);

        var rows = entries.ToArray();
        var errors = new List<string>(_manifestValidator.Validate(manifest).Errors);

        if (rows.Length == 0)
            errors.Add("DATAPACK_ENTRIES_EMPTY");

        foreach (var row in rows)
        {
            if (string.IsNullOrWhiteSpace(row.Code)) errors.Add("ENTRY_CODE_MISSING");
            if (string.IsNullOrWhiteSpace(row.Name)) errors.Add($"ENTRY_NAME_MISSING:{row.Code}");
            if (string.IsNullOrWhiteSpace(row.Unit)) errors.Add($"ENTRY_UNIT_MISSING:{row.Code}");
            if (string.IsNullOrWhiteSpace(row.ResourceType)) errors.Add($"ENTRY_RESOURCE_TYPE_MISSING:{row.Code}");
            if (string.IsNullOrWhiteSpace(row.SourceDocument)) errors.Add($"ENTRY_SOURCE_DOCUMENT_MISSING:{row.Code}");
            if (string.IsNullOrWhiteSpace(row.SourcePage)) errors.Add($"ENTRY_SOURCE_PAGE_MISSING:{row.Code}");
            if (string.IsNullOrWhiteSpace(row.SourceHash)) errors.Add($"ENTRY_SOURCE_HASH_MISSING:{row.Code}");
            if (row.EffectiveFrom == default) errors.Add($"ENTRY_EFFECTIVE_FROM_MISSING:{row.Code}");
        }

        var calculatedHash = CalculateAggregateHash(rows);
        if (!string.Equals(calculatedHash, manifest.AggregateSourceHash, StringComparison.OrdinalIgnoreCase))
            errors.Add("AGGREGATE_SOURCE_HASH_MISMATCH");

        return new DataPackIntegrityResult(errors.Count == 0, calculatedHash, errors);
    }

    public static string CalculateAggregateHash(IEnumerable<DataPackEntry> entries)
    {
        var canonical = string.Join("\n", entries
            .OrderBy(x => x.ResourceType, StringComparer.Ordinal)
            .ThenBy(x => x.Code, StringComparer.Ordinal)
            .ThenBy(x => x.SourceDocument, StringComparer.Ordinal)
            .ThenBy(x => x.SourcePage, StringComparer.Ordinal)
            .Select(x => string.Join("|", x.Code, x.Name, x.Unit, x.ResourceType,
                x.Quantity.ToString("R"), x.SourceDocument, x.SourcePage, x.SourceHash,
                x.EffectiveFrom.ToUniversalTime().ToString("O"), x.EffectiveTo?.ToUniversalTime().ToString("O") ?? "")));
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
    }
}

public sealed record DataPackIntegrityResult(bool IsProductionReady, string CalculatedAggregateHash, IReadOnlyList<string> Errors);
