namespace DTBK.Domain;

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
