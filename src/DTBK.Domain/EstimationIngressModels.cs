using System.Collections.ObjectModel;

namespace DTBK.Domain;

/// <summary>Canonical data kinds accepted by the estimation ingestion pipeline.</summary>
public enum EstimationDataKind
{
    Project,
    BoqItem,
    Norm,
    Material,
    Labor,
    Machine,
    Fuel,
    Transport,
    Coefficient,
    Formula,
    Summary,
    LegalBasis,
    Metadata,
    Provenance,
    Unknown
}

public sealed record DataSourceRef(
    string SourceId,
    string? FileName = null,
    string? Sheet = null,
    string? Address = null,
    string? Version = null,
    string? Hash = null);

public sealed record ParsedField(
    string Name,
    string? RawValue,
    string? NormalizedValue,
    string? DataType,
    DataSourceRef Source);

public sealed record ParsedRecord(
    EstimationDataKind Kind,
    string RecordKey,
    IReadOnlyDictionary<string, ParsedField> Fields,
    DataSourceRef Source,
    int Sequence = 0);

public sealed class ParsedDataset
{
    private readonly List<ParsedRecord> _records = new();
    public IReadOnlyList<ParsedRecord> Records => new ReadOnlyCollection<ParsedRecord>(_records);
    public void Add(ParsedRecord record) => _records.Add(record);
}

/// <summary>Normalized project work item used by the workbook and calculation state.</summary>
public sealed record NormalizedBoqItem(
    string ItemId,
    string? NormCode,
    string Description,
    string Unit,
    decimal Quantity,
    string? GroupKey,
    DataSourceRef Source);

public sealed record NormDefinition(
    string Code,
    string Name,
    string Unit,
    string Version,
    DateOnly? EffectiveDate,
    string? LegalBasis,
    IReadOnlyList<NormComponent> Components,
    DataSourceRef Source);

public sealed record NormComponent(
    EstimationDataKind ResourceKind,
    string ResourceCode,
    string Name,
    string Unit,
    decimal Coefficient,
    string? Group,
    DataSourceRef Source);

public sealed record PriceContext(
    string LocationCode,
    DateOnly PriceDate,
    string Version,
    string? LegalBasis);

public sealed record PricedResource(
    string Code,
    EstimationDataKind Kind,
    string Name,
    string Unit,
    decimal UnitPrice,
    PriceContext Context,
    DataSourceRef Source);

public sealed record TransportDefinition(
    string Code,
    string Name,
    string Unit,
    string Mode,
    decimal? Distance,
    decimal UnitRate,
    decimal Coefficient,
    PriceContext Context,
    DataSourceRef Source);
