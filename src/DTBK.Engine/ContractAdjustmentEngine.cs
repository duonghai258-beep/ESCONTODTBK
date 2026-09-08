using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using DTBK.Domain;

namespace DTBK.Engine;

public sealed class ContractAdjustmentEngine : IContractAdjustmentEngine
{
    public ContractAdjustmentResult Calculate(ContractAdjustmentRequest request)
    {
        if (request.OriginalContractValue < 0) throw new ArgumentOutOfRangeException(nameof(request.OriginalContractValue));
        if (string.IsNullOrWhiteSpace(request.BasePeriod) || string.IsNullOrWhiteSpace(request.AdjustmentPeriod)) throw new ArgumentException("Phải có kỳ gốc và kỳ điều chỉnh.");
        var method = request.AdjustmentMethod.Trim().ToUpperInvariant(); var breakdown = new List<string>(); decimal factor; string formula;
        switch (method)
        {
            case "MATERIAL_LABOR_MACHINE_GEOMETRIC_MEAN":
                RequirePair(request.MaterialIndex, request.BaseMaterialIndex, "VL"); RequirePair(request.LaborIndex, request.BaseLaborIndex, "NC"); RequirePair(request.MachineIndex, request.BaseMachineIndex, "MTC");
                var ivl = request.MaterialIndex!.Value / request.BaseMaterialIndex!.Value; var inc = request.LaborIndex!.Value / request.BaseLaborIndex!.Value; var imt = request.MachineIndex!.Value / request.BaseMachineIndex!.Value; factor = DecimalNthRoot(ivl * inc * imt, 3); formula = "GEOMEAN_3(VL,NC,MTC)"; breakdown.Add($"I_VL={ivl.ToString("0.########", CultureInfo.InvariantCulture)}"); breakdown.Add($"I_NC={inc.ToString("0.########", CultureInfo.InvariantCulture)}"); breakdown.Add($"I_MTC={imt.ToString("0.########", CultureInfo.InvariantCulture)}"); break;
            case "MATERIAL_LABOR_GEOMETRIC_MEAN":
                RequirePair(request.MaterialIndex, request.BaseMaterialIndex, "VL"); RequirePair(request.LaborIndex, request.BaseLaborIndex, "NC"); var ivl2 = request.MaterialIndex!.Value / request.BaseMaterialIndex!.Value; var inc2 = request.LaborIndex!.Value / request.BaseLaborIndex!.Value; factor = DecimalNthRoot(ivl2 * inc2, 2); formula = "GEOMEAN_2(VL,NC)"; breakdown.Add($"I_VL={ivl2.ToString("0.########", CultureInfo.InvariantCulture)}"); breakdown.Add($"I_NC={inc2.ToString("0.########", CultureInfo.InvariantCulture)}"); break;
            case "CUSTOM_FACTOR": throw new NotSupportedException("CUSTOM_FACTOR không được phép nhập trực tiếp; phải có phương pháp và bằng chứng được cấu hình trong hợp đồng.");
            default: throw new ArgumentException($"Phương pháp điều chỉnh chưa được hỗ trợ: {request.AdjustmentMethod}");
        }
        if (factor <= 0 || decimal.MaxValue / factor < request.OriginalContractValue) throw new InvalidOperationException("Hệ số điều chỉnh không hợp lệ hoặc vượt giới hạn số học.");
        var adjusted = decimal.Round(request.OriginalContractValue * factor, 0, MidpointRounding.AwayFromZero); var amount = adjusted - request.OriginalContractValue; var evidence = CalculationAuditHash(request, factor, adjusted); var legalBasis = request.LegalReferenceId?.ToString() ?? "REVIEW_REQUIRED";
        return new ContractAdjustmentResult(request.OriginalContractValue, factor, adjusted, amount, formula, request.LegalReferenceId.HasValue ? "REVIEW_REQUIRED" : "NOT_ASSESSED", breakdown, evidence, legalBasis);
    }
    private static void RequirePair(decimal? value, decimal? baseValue, string code) { if (!value.HasValue || !baseValue.HasValue || baseValue.Value <= 0 || value.Value <= 0) throw new InvalidOperationException($"Thiếu chỉ số hoặc chỉ số gốc hợp lệ cho {code}."); }
    private static decimal DecimalNthRoot(decimal value, int n) { if (value <= 0) throw new ArgumentOutOfRangeException(nameof(value)); var x = (decimal)Math.Pow((double)value, 1d / n); for (var i = 0; i < 8; i++) x = ((n - 1) * x + value / Pow(x, n - 1)) / n; return x; }
    private static decimal Pow(decimal value, int exponent) { var result = 1m; for (var i = 0; i < exponent; i++) result *= value; return result; }
    private static string CalculationAuditHash(ContractAdjustmentRequest request, decimal factor, decimal adjusted) { var canonical = string.Join("|", request.OriginalContractValue.ToString(CultureInfo.InvariantCulture), request.BasePeriod, request.AdjustmentPeriod, request.AdjustmentMethod, request.MaterialIndex?.ToString(CultureInfo.InvariantCulture) ?? "", request.LaborIndex?.ToString(CultureInfo.InvariantCulture) ?? "", request.MachineIndex?.ToString(CultureInfo.InvariantCulture) ?? "", request.BaseMaterialIndex?.ToString(CultureInfo.InvariantCulture) ?? "", request.BaseLaborIndex?.ToString(CultureInfo.InvariantCulture) ?? "", request.BaseMachineIndex?.ToString(CultureInfo.InvariantCulture) ?? "", factor.ToString(CultureInfo.InvariantCulture), adjusted.ToString(CultureInfo.InvariantCulture)); return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical))); }
}
