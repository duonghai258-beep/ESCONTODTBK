using System.Text.Json.Serialization;

namespace DTBK.Domain;

/// <summary>Canonical shared contract translated from the OCE BOQ position shape.</summary>
public sealed record DTBKPositionContract(
    [property: JsonPropertyName("id")] string? Id,
    [property: JsonPropertyName("boq_id")] string? BoqId,
    [property: JsonPropertyName("parent_id")] string? ParentId,
    [property: JsonPropertyName("ordinal")] string Ordinal,
    [property: JsonPropertyName("description")] string Description,
    [property: JsonPropertyName("unit")] string Unit,
    [property: JsonPropertyName("quantity")] decimal Quantity,
    [property: JsonPropertyName("unit_rate")] decimal UnitRate,
    [property: JsonPropertyName("source")] string? Source,
    [property: JsonPropertyName("confidence")] decimal? Confidence,
    [property: JsonPropertyName("assembly_id")] string? AssemblyId,
    [property: JsonPropertyName("cad_element_ids")] IReadOnlyList<string>? CadElementIds,
    [property: JsonPropertyName("metadata")] IReadOnlyDictionary<string, object?>? Metadata);

public sealed record DTBKPositionBatch(
    IReadOnlyList<DTBKPositionContract> Positions,
    string? SourceFile,
    string SourceHash);

public sealed record DTBKCapabilityMap(
    bool Boq,
    bool CostDatabase,
    bool CadBimTakeoff,
    bool Validation,
    bool Reporting,
    bool Scheduling,
    bool AiEstimation,
    bool ApiBackend,
    IReadOnlyList<string> EvidencePaths);

/// <summary>Application-facing import contract; implementations belong to Import.</summary>
public interface IDTBKImportService
{
    DTBKPositionBatch ReadPositionsJson(string filePath);
    IReadOnlyList<CostLine> ImportPositionsAsCostLines(string filePath);
    DTBKCapabilityMap InspectCapabilities(string? repositoryRoot = null);
}

/// <summary>Persistence/runtime contract; implementation belongs to Infrastructure.</summary>
public interface IDTBKRuntimeStore
{
    void SaveImportedBatch(DTBKPositionBatch batch);
    DTBKPositionBatch? LoadImportedBatch();
}
