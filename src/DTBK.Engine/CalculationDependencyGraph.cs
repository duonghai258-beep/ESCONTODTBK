namespace DTBK.Engine;

/// <summary>
/// Canonical dependency invalidation contract for S01-S15 and T01-T08.
/// It tracks invalidation only; calculation remains owned by the canonical engines.
/// </summary>
public static class CalculationDependencyGraph
{
    private static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> Dependents =
        new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["S01"] = new[] { "S02", "S04", "S05", "S06", "S07", "S11", "S12", "S13" },
            ["S02"] = new[] { "S03" },
            ["S03"] = new[] { "S04", "S05", "S06", "T01", "T02", "T03" },
            ["S04"] = new[] { "S08", "T01" },
            ["S05"] = new[] { "S08", "T02" },
            ["S06"] = new[] { "S08", "T03" },
            ["S07"] = new[] { "S08", "T04" },
            ["S08"] = new[] { "S09", "S10", "T05" },
            ["S09"] = new[] { "S10", "T05" },
            ["S10"] = new[] { "S11", "S12", "S13", "S14", "T06", "T07" },
            ["S11"] = new[] { "S12", "S14", "T06", "T07" },
            ["S12"] = new[] { "S13", "S14", "T07" },
            ["S13"] = new[] { "S14", "T07" },
            ["S14"] = new[] { "S15", "T08" },
            ["S15"] = new[] { "T08" },
            ["T01"] = new[] { "T05", "T08" },
            ["T02"] = new[] { "T05", "T08" },
            ["T03"] = new[] { "T05", "T08" },
            ["T04"] = new[] { "T05", "T08" },
            ["T05"] = new[] { "T06", "T08" },
            ["T06"] = new[] { "T07", "T08" },
            ["T07"] = new[] { "T08" }
        };

    public static IReadOnlySet<string> Invalidate(IEnumerable<string> changedNodes)
    {
        ArgumentNullException.ThrowIfNull(changedNodes);
        var invalidated = new HashSet<string>(changedNodes.Where(IsKnownNode), StringComparer.OrdinalIgnoreCase);
        var queue = new Queue<string>(invalidated);
        while (queue.Count > 0)
        {
            var node = queue.Dequeue();
            if (!Dependents.TryGetValue(node, out var children)) continue;
            foreach (var child in children)
                if (invalidated.Add(child)) queue.Enqueue(child);
        }

        return invalidated;
    }

    private static bool IsKnownNode(string node) =>
        !string.IsNullOrWhiteSpace(node) &&
        (node.StartsWith("S", StringComparison.OrdinalIgnoreCase) ||
         node.StartsWith("T", StringComparison.OrdinalIgnoreCase));
}

/// <summary>
/// Canonical row/cell dependency scheduler. This is the single implementation used
/// by workbook controls, formula registration and affected-only recalculation.
/// </summary>
public sealed class RecalculationScheduler
{
    private readonly Dictionary<string, HashSet<string>> _edges = new(StringComparer.OrdinalIgnoreCase);

    public void AddDependency(string source, string target, string kind = "FORMULA")
    {
        if (string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(target))
            throw new ArgumentException("Dependency source/target is required.");
        if (!_edges.TryGetValue(source, out var targets))
            _edges[source] = targets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        targets.Add(target);
    }

    public void AddFormulaDependencies(string target, IEnumerable<string> dependencies)
    {
        ArgumentNullException.ThrowIfNull(dependencies);
        foreach (var dependency in dependencies)
            AddDependency(dependency, target, "FORMULA");
    }

    /// <summary>
    /// Registers dependencies emitted by the evidence-gated formula runtime.
    /// No value is invented here; only dependencies already emitted by the provider are registered.
    /// </summary>
    public void RegisterFormulaResult(string target, FormulaValueResult result)
    {
        if (string.IsNullOrWhiteSpace(target))
            throw new ArgumentException("Formula target is required.", nameof(target));
        ArgumentNullException.ThrowIfNull(result);
        AddFormulaDependencies(target, result.Dependencies.Select(x => x.Key));
    }

    public RecalculationPlan Plan(IEnumerable<string> changedNodes)
    {
        ArgumentNullException.ThrowIfNull(changedNodes);
        var changed = changedNodes.Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        var affected = new HashSet<string>(changed, StringComparer.OrdinalIgnoreCase);
        var queue = new Queue<string>(changed);
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (!_edges.TryGetValue(current, out var next)) continue;
            foreach (var target in next)
                if (affected.Add(target)) queue.Enqueue(target);
        }

        var order = TopologicalOrder(affected);
        return new RecalculationPlan(affected.ToArray(), order);
    }

    private IReadOnlyList<string> TopologicalOrder(HashSet<string> nodes)
    {
        var indegree = nodes.ToDictionary(n => n, _ => 0, StringComparer.OrdinalIgnoreCase);
        foreach (var source in nodes)
            if (_edges.TryGetValue(source, out var targets))
                foreach (var target in targets)
                    if (nodes.Contains(target)) indegree[target]++;

        var ready = new SortedSet<string>(indegree.Where(x => x.Value == 0).Select(x => x.Key), StringComparer.OrdinalIgnoreCase);
        var result = new List<string>(nodes.Count);
        while (ready.Count > 0)
        {
            var n = ready.Min!;
            ready.Remove(n);
            result.Add(n);
            if (!_edges.TryGetValue(n, out var targets)) continue;
            foreach (var target in targets.Where(nodes.Contains))
                if (--indegree[target] == 0) ready.Add(target);
        }

        if (result.Count != nodes.Count)
            throw new InvalidOperationException("Circular calculation dependency detected.");
        return result;
    }
}

public sealed record RecalculationPlan(
    IReadOnlyList<string> Invalidated,
    IReadOnlyList<string> EvaluationOrder);
