using DTBK.Domain;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace DTBK.Engine;

public sealed class SubcontractorQuoteEngine
{
    public QuoteComparison Compare(string tenderId, IEnumerable<SubcontractorQuote> quotes)
    {
        if (string.IsNullOrWhiteSpace(tenderId)) throw new ArgumentException("TenderId is required.", nameof(tenderId));
        ArgumentNullException.ThrowIfNull(quotes);

        var accepted = quotes
            .Where(q => q.Status is QuoteStatus.Submitted or QuoteStatus.Accepted)
            .GroupBy(q => q.SubcontractorId, StringComparer.Ordinal)
            .Select(group => group
                .OrderByDescending(q => q.Status == QuoteStatus.Accepted)
                .ThenByDescending(q => q.SubmittedAtUtc)
                .ThenByDescending(q => q.QuoteId, StringComparer.Ordinal)
                .First())
            .ToList();
        if (accepted.Count == 0) throw new InvalidOperationException("At least one submitted or accepted quotation is required.");

        var byWork = accepted.SelectMany(q => q.Lines.Select(l => (q, l)))
            .GroupBy(x => (x.l.WorkCode, x.l.Unit), StringComparerTupleComparer.Instance);
        var lines = new List<QuoteComparisonLine>();
        foreach (var group in byWork)
        {
            var quantity = group
                .GroupBy(x => x.q.SubcontractorId, StringComparer.Ordinal)
                .Select(g => g.Sum(x => x.l.Quantity))
                .DefaultIfEmpty(0m)
                .Max();
            if (quantity <= 0m)
                throw new InvalidOperationException($"Quote group {group.Key.WorkCode}/{group.Key.Unit} has no positive quantity.");

            var rates = new Dictionary<string, decimal>(StringComparer.Ordinal);
            foreach (var quoteGroup in group.GroupBy(x => x.q.SubcontractorId, StringComparer.Ordinal))
            {
                var totalQuantity = quoteGroup.Sum(x => x.l.Quantity);
                var totalAmount = quoteGroup.Sum(x => x.l.ResolvedAmount);
                if (totalQuantity <= 0m)
                    throw new InvalidOperationException($"Quotation {quoteGroup.Key} has non-positive quantity for {group.Key.WorkCode}/{group.Key.Unit}.");
                rates[quoteGroup.Key] = totalAmount / totalQuantity;
            }

            var best = rates.OrderBy(x => x.Value).ThenBy(x => x.Key, StringComparer.Ordinal).First();
            lines.Add(new(group.Key.WorkCode, group.Key.Unit, quantity, rates, best.Key, best.Value));
        }

        var totals = accepted.ToDictionary(
            q => q.SubcontractorId,
            q => q.Lines.Sum(x => x.ResolvedAmount),
            StringComparer.Ordinal);
        var evidencePayload = JsonSerializer.Serialize(new { tenderId, lines, totals });
        var evidenceHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(evidencePayload)));
        return new QuoteComparison(tenderId, lines.OrderBy(x => x.WorkCode, StringComparer.Ordinal).ToList(), totals, evidenceHash);
    }

    private sealed class StringComparerTupleComparer : IEqualityComparer<(string WorkCode, string Unit)>
    {
        public static readonly StringComparerTupleComparer Instance = new();
        public bool Equals((string WorkCode, string Unit) x, (string WorkCode, string Unit) y) =>
            StringComparer.Ordinal.Equals(x.WorkCode, y.WorkCode) && StringComparer.Ordinal.Equals(x.Unit, y.Unit);
        public int GetHashCode((string WorkCode, string Unit) obj) => HashCode.Combine(StringComparer.Ordinal.GetHashCode(obj.WorkCode), StringComparer.Ordinal.GetHashCode(obj.Unit));
    }
}
