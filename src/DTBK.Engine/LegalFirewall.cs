using System.Security.Cryptography;
using System.Text;
using DTBK.Domain;

namespace DTBK.Engine;

public sealed record LegalEvidence(
    string EvidenceId,
    string Source,
    string SourceUri,
    string Version,
    DateTime? EffectiveFrom,
    DateTime? EffectiveTo,
    string ContentHash,
    bool IsVerified);

public sealed record LegalFirewallDecision(
    string RuleCode,
    bool Allowed,
    string Reason,
    string EvidenceHash,
    DateTime EvaluatedAtUtc);

/// <summary>
/// Legal boundary between AI/UI suggestions and deterministic calculation.
/// It does not decide whether a legal source is legally correct; it enforces
/// that calculations requiring legal evidence cannot silently use unverified data.
/// </summary>
public sealed class LegalFirewall
{
    public LegalFirewallDecision Evaluate(string ruleCode, DateTime calculationDate, LegalEvidence? evidence, bool requiresEvidence = true)
    {
        if (!requiresEvidence)
            return Decide(ruleCode, true, "Rule không yêu cầu căn cứ pháp lý.", evidence);

        if (evidence is null)
            return Decide(ruleCode, false, "Thiếu evidence pháp lý; Engine không được tự suy đoán.", null);

        if (!evidence.IsVerified)
            return Decide(ruleCode, false, "Evidence chưa được xác minh.", evidence);

        if (string.IsNullOrWhiteSpace(evidence.Source) || string.IsNullOrWhiteSpace(evidence.Version))
            return Decide(ruleCode, false, "Evidence thiếu nguồn hoặc phiên bản.", evidence);

        if (evidence.EffectiveFrom.HasValue && calculationDate < evidence.EffectiveFrom.Value.Date)
            return Decide(ruleCode, false, "Căn cứ chưa có hiệu lực tại ngày tính.", evidence);

        if (evidence.EffectiveTo.HasValue && calculationDate.Date > evidence.EffectiveTo.Value.Date)
            return Decide(ruleCode, false, "Căn cứ đã hết hiệu lực tại ngày tính.", evidence);

        if (string.IsNullOrWhiteSpace(evidence.ContentHash))
            return Decide(ruleCode, false, "Evidence chưa có content hash.", evidence);

        return Decide(ruleCode, true, "Evidence hợp lệ về mặt kiểm soát provenance/hiệu lực; cần kiểm tra nghiệp vụ trước phát hành.", evidence);
    }

    private static LegalFirewallDecision Decide(string code, bool allowed, string reason, LegalEvidence? evidence)
    {
        var payload = string.Join("|", code, reason, evidence?.EvidenceId ?? "", evidence?.Source ?? "", evidence?.Version ?? "", evidence?.ContentHash ?? "");
        return new LegalFirewallDecision(code, allowed, reason, Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload))), DateTime.UtcNow);
    }
}
