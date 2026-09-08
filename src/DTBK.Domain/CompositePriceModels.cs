namespace DTBK.Domain;

public sealed record CompositePriceComponent(
    string ComponentCode,
    string Unit,
    decimal Factor,
    decimal ComponentUnitPrice,
    CalculationProvenance Provenance)
{
    public decimal Amount => Factor * ComponentUnitPrice;
}

public sealed record CompositePriceDefinition(
    string CompositeCode,
    string Unit,
    IReadOnlyList<CompositePriceComponent> Components,
    CalculationProvenance Provenance);

public sealed record CompositePriceResult(
    string CompositeCode,
    string Unit,
    decimal UnitPrice,
    IReadOnlyList<CompositePriceComponent> Components,
    CalculationTrace Trace,
    VerificationStatus Status);
