using DTBK.Domain;
namespace DTBK.Engine;
public sealed record IntegrationPathEvidence(string Stage, string Component, string Target, bool IsCanonical, string Note);
public sealed record IntegrationAuditResult(bool Complete, IReadOnlyList<IntegrationPathEvidence> Evidence, IReadOnlyList<string> Gaps);
public sealed class CanonicalIntegrationAudit
{
    private static readonly string[] RequiredStages = { "control/input", "importer", "cell", "norm/price/transport", "CalculationStateEngine.Calculate", "state", "15-sheet projection", "report/export/audit" };
    public IntegrationAuditResult Evaluate(IEnumerable<IntegrationPathEvidence> evidence)
    {
        var rows = evidence.ToArray();
        var gaps = RequiredStages.Where(stage => !rows.Any(x => x.Stage.Equals(stage, StringComparison.OrdinalIgnoreCase) && x.IsCanonical)).ToArray();
        return new IntegrationAuditResult(gaps.Length == 0, rows, gaps);
    }
}
