using System.Security.Cryptography;
using System.Text;
using DTBK.Domain;
namespace DTBK.Engine;
public sealed class MachineEngine
{
    private const string Version = "2.7.0-MACHINE-1"; private readonly IMachineResolver _resolver;
    public MachineEngine(IMachineResolver resolver) => _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
    public MachineRateResult Calculate(MachineRateRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.MachineCode)) throw new ArgumentException("MachineCode is required.", nameof(request)); if (request.ProvinceId <= 0) throw new ArgumentOutOfRangeException(nameof(request.ProvinceId));
        var (spec, prices) = _resolver.Resolve(request); if (spec.UsefulLifeHours <= 0) throw new InvalidOperationException("Machine useful life must be positive."); if (spec.PurchaseCost < 0 || spec.SalvageValue < 0 || spec.SalvageValue > spec.PurchaseCost) throw new InvalidOperationException("Machine purchase/salvage values are invalid."); if (spec.RepairRate < 0 || spec.FuelConsumptionPerHour < 0 || spec.OperatorHoursPerHour < 0 || spec.OtherHourlyCost < 0) throw new InvalidOperationException("Machine cost components cannot be negative.");
        var depreciation = (spec.PurchaseCost - spec.SalvageValue) / spec.UsefulLifeHours; var repair = spec.PurchaseCost * spec.RepairRate; var fuel = spec.FuelConsumptionPerHour * prices.FuelUnitPrice; var labor = spec.OperatorHoursPerHour * prices.OperatorHourlyRate; var total = depreciation + repair + fuel + labor + spec.OtherHourlyCost;
        var inputHash = Hash(string.Join("|", spec.MachineCode, request.EffectiveDate.ToString("O"), spec.SourceHash)); var outputHash = Hash(string.Join("|", depreciation, repair, fuel, labor, spec.OtherHourlyCost, total));
        var trace = new CalculationTrace("MACHINE_RATE", inputHash, outputHash, Version, new[] { new CalculationTraceStep("INPUT", "Machine", spec.MachineCode, spec.SourceReference, spec.SourceHash, "MACHINE_INPUT"), new CalculationTraceStep("RULE", "Depreciation", "(purchase - salvage) / usefulLifeHours", spec.SourceReference, spec.SourceHash, "MACHINE_DEPRECIATION"), new CalculationTraceStep("RULE", "Repair", "purchase * repairRate", spec.SourceReference, spec.SourceHash, "MACHINE_REPAIR"), new CalculationTraceStep("RESOLVER", "Fuel/Labor", prices.PriceSource, prices.PriceSource, prices.PriceSourceHash, "MACHINE_PRICE"), new CalculationTraceStep("OUTPUT", "TotalPerHour", total.ToString("0.##"), prices.PriceSource, prices.PriceSourceHash, "MACHINE_RATE") });
        return new(spec.MachineCode, depreciation, repair, fuel, labor, spec.OtherHourlyCost, total, request.Currency, "CALCULATION_PASS", trace);
    }
    public CalculationOutcome<MachineRateResult> TryCalculate(MachineRateRequest request) { try { var result = Calculate(request); return new(result, DTBK.Domain.VerificationStatus.NeedsVerification, "Machine result requires verified official source provenance.", result.Trace); } catch (InvalidOperationException ex) { return new(null, DTBK.Domain.VerificationStatus.Blocked, ex.Message, null); } }
    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}
