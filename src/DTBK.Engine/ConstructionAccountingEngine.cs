using DTBK.Domain;

namespace DTBK.Engine;

/// <summary>
/// Hỗ trợ tính toán thông tin ghi nhận doanh thu/chi phí hợp đồng xây dựng theo
/// mô hình VAS 15. Đây là lớp tính toán hỗ trợ, không tự ghi sổ kế toán và không
/// thay thế phán đoán của kế toán về khả năng ước tính đáng tin cậy.
/// </summary>
public sealed class ConstructionAccountingEngine : IConstructionAccountingEngine
{
    public AccountingRecognitionResult Calculate(AccountingRecognitionRequest request)
    {
        if (request.ContractRevenue < 0) throw new ArgumentOutOfRangeException(nameof(request.ContractRevenue));
        if (request.CumulativeCostsToDate < 0 || request.EstimatedTotalContractCosts < 0 || request.PriorRecognizedRevenue < 0 || request.CurrentPeriodCosts < 0)
            throw new ArgumentOutOfRangeException("Chi phí/doanh thu không được âm.");

        var method = request.MeasurementMethod.Trim().ToUpperInvariant();
        decimal completion;
        var notes = new List<string>();

        if (!request.OutcomeReliablyEstimable)
        {
            if (!request.CostsRecoverable)
                return new AccountingRecognitionResult("VAS15", method, 0m, 0m, 0m, request.CurrentPeriodCosts, 0m,
                    Math.Max(0m, request.EstimatedTotalContractCosts - request.ContractRevenue), true,
                    "REVIEW_REQUIRED", new[] { "Kết quả hợp đồng chưa ước tính đáng tin cậy và chi phí chưa được xác định là có khả năng thu hồi." });

            var revenue = request.CurrentPeriodCosts;
            return new AccountingRecognitionResult("VAS15", method, 0m, request.PriorRecognizedRevenue + revenue,
                revenue, request.CurrentPeriodCosts, 0m, 0m, false, "REVIEW_REQUIRED",
                new[] { "Doanh thu tạm tính bằng chi phí kỳ này có khả năng thu hồi; cần kế toán xác nhận hồ sơ." });
        }

        switch (method)
        {
            case "COST_TO_COST":
                if (request.EstimatedTotalContractCosts <= 0m) throw new ArgumentException("Tổng chi phí dự toán phải lớn hơn 0.");
                completion = request.CumulativeCostsToDate / request.EstimatedTotalContractCosts;
                break;
            case "ASSESSMENT":
                if (request.AssessedCompletionPercent is null) throw new ArgumentException("Thiếu tỷ lệ hoàn thành được đánh giá.");
                completion = request.AssessedCompletionPercent.Value;
                break;
            case "QUANTITY":
                if (request.CompletedQuantity is null || request.TotalQuantity is null || request.TotalQuantity <= 0m)
                    throw new ArgumentException("Thiếu khối lượng hoàn thành/tổng khối lượng.");
                completion = request.CompletedQuantity.Value / request.TotalQuantity.Value;
                break;
            default:
                throw new ArgumentException($"Phương pháp xác định phần công việc hoàn thành chưa hỗ trợ: {request.MeasurementMethod}");
        }

        if (completion < 0m || completion > 1m) throw new ArgumentOutOfRangeException(nameof(request), "Tỷ lệ hoàn thành phải từ 0 đến 100%.");

        var cumulativeRevenue = decimal.Round(request.ContractRevenue * completion, 0, MidpointRounding.AwayFromZero);
        var currentRevenue = cumulativeRevenue - request.PriorRecognizedRevenue;
        if (currentRevenue < 0m) currentRevenue = 0m;

        var expectedLoss = Math.Max(0m, request.EstimatedTotalContractCosts - request.ContractRevenue);
        var lossImmediate = expectedLoss > 0m;
        var gross = currentRevenue - request.CurrentPeriodCosts;
        if (lossImmediate) notes.Add("Tổng chi phí dự kiến vượt doanh thu hợp đồng: cần xem xét ghi nhận khoản lỗ dự kiến theo chính sách kế toán áp dụng.");
        notes.Add("Kết quả là thông tin hỗ trợ; việc ghi nhận sổ kế toán cần căn cứ hồ sơ nghiệm thu, hợp đồng và chính sách kế toán của doanh nghiệp.");

        return new AccountingRecognitionResult(
            "VAS15", method, completion, cumulativeRevenue, currentRevenue, request.CurrentPeriodCosts,
            gross, expectedLoss, lossImmediate, "REVIEW_REQUIRED", notes);
    }
}
