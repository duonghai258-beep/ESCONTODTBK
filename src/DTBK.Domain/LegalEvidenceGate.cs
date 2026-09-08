namespace DTBK.Domain;
public sealed record LegalRowEvidence(string SourceId, string Document, string? Appendix, string? Table, string? Row, DateOnly? EffectiveDate, string ContentHash);
public sealed record LegalActivationResult(bool Activated, IReadOnlyList<string> BlockingReasons, IReadOnlyList<LegalRowEvidence> Evidence);
public sealed class LegalEvidenceGate
{
    public LegalActivationResult Evaluate(IEnumerable<LegalRowEvidence> evidence, DateOnly asOf)
    {
        var rows = evidence.Where(x => x.EffectiveDate is null || x.EffectiveDate <= asOf).ToArray();
        var blockers = new List<string>();
        if (rows.Length == 0) blockers.Add("No effective row-level legal evidence supplied.");
        foreach (var row in rows)
        {
            if (string.IsNullOrWhiteSpace(row.SourceId)) blockers.Add("Legal evidence is missing SourceId.");
            if (string.IsNullOrWhiteSpace(row.Document)) blockers.Add("Legal evidence is missing Document.");
            if (string.IsNullOrWhiteSpace(row.ContentHash)) blockers.Add($"Legal evidence for {row.Document} is missing ContentHash.");
        }
        return new LegalActivationResult(blockers.Count == 0, blockers.Distinct().ToArray(), rows);
    }
}
