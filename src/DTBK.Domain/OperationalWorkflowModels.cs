namespace DTBK.Domain;

public enum RoadConditionType { Paved = 1, Unpaved = 2, Mountain = 3, Restricted = 4 }
public enum TransportMethod { Road = 1, Rail = 2, Water = 3, Combined = 4 }

public sealed record TransportRoute(
    string RouteCode,
    string Origin,
    string Destination,
    RoadConditionType RoadCondition,
    decimal DistanceKm,
    string SourceReference,
    string SourceHash,
    DateTime EffectiveFrom,
    DateTime? EffectiveTo = null);

public sealed record VehicleSpecification(
    string VehicleCode,
    string VehicleName,
    ResourceType EnergyType,
    decimal Capacity,
    string CapacityUnit,
    decimal HoursPerDistanceUnit,
    decimal OtherHourlyCost,
    string SourceReference,
    string SourceHash,
    DateTime EffectiveFrom,
    DateTime? EffectiveTo = null);

public sealed record TransportNorm(
    string NormCode,
    string MaterialCode,
    string VehicleCode,
    RoadConditionType RoadCondition,
    decimal DistanceUnitKm,
    decimal FuelPerDistanceUnit,
    decimal OperatorHoursPerDistanceUnit,
    string FuelCode,
    string OperatorLaborGroup,
    string SourceReference,
    string SourceHash,
    DateTime EffectiveFrom,
    DateTime? EffectiveTo = null);

public sealed record TransportRequest(
    string MaterialCode,
    decimal Quantity,
    string QuantityUnit,
    string Origin,
    string Destination,
    string RouteCode,
    RoadConditionType RoadCondition,
    string VehicleCode,
    DateTime EffectiveDate,
    int ProvinceId,
    string Currency = "VND",
    TransportMethod Method = TransportMethod.Road);

public sealed record TransportPriceSet(
    decimal VehicleHourlyRate,
    decimal FuelUnitPrice,
    decimal OperatorHourlyRate,
    string PriceSource,
    string PriceSourceHash,
    bool TransportIncluded = false);

public sealed record TransportResolvedInputs(
    TransportRoute Route,
    VehicleSpecification Vehicle,
    TransportNorm Norm,
    TransportPriceSet Prices);

public sealed record CalculationTraceStep(
    string Stage,
    string Key,
    string Value,
    string SourceReference,
    string SourceHash,
    string RuleCode);

public sealed record CalculationTrace(
    string CalculationCode,
    string InputHash,
    string OutputHash,
    string EngineVersion,
    IReadOnlyList<CalculationTraceStep> Steps);

public sealed record TransportCostResult(
    decimal Quantity,
    decimal DistanceKm,
    decimal FuelCost,
    decimal VehicleCost,
    decimal OperatorLaborCost,
    decimal OtherCost,
    decimal TotalCost,
    string Currency,
    string Status,
    CalculationTrace Trace);

public sealed record MachineSpecification(
    string MachineCode,
    string MachineName,
    string CapacityUnit,
    decimal Capacity,
    decimal UsefulLifeHours,
    decimal PurchaseCost,
    decimal SalvageValue,
    decimal RepairRate,
    decimal FuelConsumptionPerHour,
    string FuelCode,
    decimal OperatorHoursPerHour,
    string OperatorLaborGroup,
    decimal OtherHourlyCost,
    string SourceReference,
    string SourceHash,
    DateTime EffectiveFrom,
    DateTime? EffectiveTo = null);

public sealed record MachinePriceSet(
    decimal FuelUnitPrice,
    decimal OperatorHourlyRate,
    string PriceSource,
    string PriceSourceHash);

public sealed record MachineRateRequest(
    string MachineCode,
    DateTime EffectiveDate,
    int ProvinceId,
    string Currency = "VND");

public sealed record MachineRateResult(
    string MachineCode,
    decimal DepreciationPerHour,
    decimal RepairPerHour,
    decimal FuelPerHour,
    decimal OperatorLaborPerHour,
    decimal OtherPerHour,
    decimal TotalPerHour,
    string Currency,
    string Status,
    CalculationTrace Trace);

public sealed record RateOverride(
    string OverrideCode,
    string Scope,
    string TargetCode,
    decimal Value,
    string Unit,
    DateTime EffectiveFrom,
    DateTime? EffectiveTo,
    string Reason,
    string SourceReference,
    string SourceHash,
    bool IsApproved);

public interface ITransportResolver
{
    TransportResolvedInputs Resolve(TransportRequest request);
}

public interface IMachineResolver
{
    (MachineSpecification Specification, MachinePriceSet Prices) Resolve(MachineRateRequest request);
}

public interface IOverrideResolver
{
    RateOverride? Resolve(string scope, string targetCode, DateTime effectiveDate);
}

public enum ExcelZoneKind { Upper = 1, Middle = 2, Lower = 3 }

public sealed record ExcelZoneDescriptor(
    string WorksheetName,
    ExcelZoneKind Zone,
    int FirstRow,
    int LastRow,
    string SourceRange,
    IReadOnlyList<string> MergeRanges,
    string? PrintArea,
    string? PageOrientation);

public sealed record ExcelThreeZoneWorkbook(
    byte[] OriginalWorkbook,
    string WorksheetName,
    int HeaderRow,
    int FirstDataRow,
    int LastDataRow,
    IReadOnlyDictionary<string, int> HeaderColumns,
    IReadOnlyList<ExcelZoneDescriptor> Zones,
    IReadOnlyList<CostLine> Lines);

public sealed record ProjectVariation(
    long ProjectId,
    string WorkCode,
    decimal QuantityDelta,
    decimal UnitPriceDelta,
    DateTime EffectiveDate,
    string SourceReference,
    string SourceHash);

public sealed record PaymentPeriod(
    long ProjectId,
    string PeriodCode,
    decimal AcceptedValue,
    decimal CurrentValue,
    decimal CumulativeValue,
    decimal PaidValue,
    DateTime EffectiveDate,
    string SourceReference,
    string SourceHash);

public sealed record ProgressPeriod(
    long ProjectId,
    string WorkCode,
    string PeriodCode,
    decimal PlannedQuantity,
    decimal ActualQuantity,
    decimal Value,
    DateTime EffectiveDate,
    string SourceReference,
    string SourceHash);

public sealed record EstimateCalculationResult(
    IReadOnlyList<CostLine> Lines,
    ProjectCostSummary Summary,
    CalculationTrace Trace);
