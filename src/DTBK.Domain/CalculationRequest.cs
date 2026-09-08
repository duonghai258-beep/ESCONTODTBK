namespace DTBK.Domain;

/// <summary>
/// Canonical input contract emitted by UI/workflow layers before calculation.
/// UI controls must populate this request instead of invoking pricing engines directly.
/// </summary>
public sealed record CalculationRequest(
    CalculationContext Context,
    IReadOnlyList<ProjectItem> Items,
    WorkType WorkType,
    CalculationScenario Scenario,
    LocationType Location,
    int ProvinceId,
    string VatCategory = "STANDARD",
    IReadOnlyDictionary<long, decimal>? TransportByItem = null,
    IReadOnlyDictionary<long, CalculationProvenance>? ProvenanceByItem = null);
