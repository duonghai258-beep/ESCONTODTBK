using DTBK.Domain;

namespace DTBK.Engine;

/// <summary>
/// Production gate for Vietnam legal/price/norm packages. It never invents missing data.
/// </summary>
public sealed class VietnamDataPackValidator
{
    public DataPackValidationResult Validate(VietnamDataPackManifest manifest)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(manifest.PackageCode)) errors.Add("PACKAGE_CODE_MISSING");
        if (string.IsNullOrWhiteSpace(manifest.Version)) errors.Add("VERSION_MISSING");
        if (manifest.SourceDocuments.Count == 0) errors.Add("SOURCE_DOCUMENTS_MISSING");
        if (string.IsNullOrWhiteSpace(manifest.AggregateSourceHash)) errors.Add("SOURCE_HASH_MISSING");
        if (!manifest.ReplacementAnnexesApplied) errors.Add("REPLACEMENT_ANNEXES_NOT_APPLIED");
        if (manifest.NormCount <= 0) errors.Add("NORM_DATA_EMPTY");
        if (manifest.NormDetailCount <= 0) errors.Add("NORM_DETAIL_DATA_EMPTY");
        if (manifest.MaterialCount <= 0) errors.Add("MATERIAL_DATA_EMPTY");
        if (manifest.LaborCount <= 0) errors.Add("LABOR_DATA_EMPTY");
        if (manifest.EquipmentCount <= 0) errors.Add("EQUIPMENT_DATA_EMPTY");
        if (!manifest.ProductionReady) errors.Add("PACKAGE_NOT_MARKED_PRODUCTION_READY");
        return new DataPackValidationResult(errors.Count == 0, errors);
    }
}

public sealed record DataPackValidationResult(bool IsProductionReady, IReadOnlyList<string> Errors);
