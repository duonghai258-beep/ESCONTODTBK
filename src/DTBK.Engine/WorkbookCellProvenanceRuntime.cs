namespace DTBK.Engine;

public enum WorkbookCellRole { Input, Calculated, Formula, Lookup, Linked, Override, ReadOnly, Invalid }

public sealed record CellProvenance(
    string SheetId,
    string TableId,
    string RowKey,
    string ColumnId,
    WorkbookCellRole Role,
    string Value,
    string? Formula,
    string? Source,
    string? Provider,
    string? Rule,
    string? CalculationCode,
    IReadOnlyList<string> Dependencies,
    DateTime RecordedAtUtc);

/// <summary>
/// Runtime provenance ledger. A cell value is explainable without recalculating it.
/// Formula/dependency engines can append records after evaluation.
/// </summary>
public sealed class WorkbookCellProvenanceRuntime
{
    private readonly Dictionary<string, CellProvenance> _cells = new(StringComparer.OrdinalIgnoreCase);

    public void Record(CellProvenance provenance)
    {
        ArgumentNullException.ThrowIfNull(provenance);
        _cells[Key(provenance.SheetId, provenance.TableId, provenance.RowKey, provenance.ColumnId)] = provenance;
    }

    public bool TryExplain(string sheetId, string tableId, string rowKey, string columnId, out CellProvenance? provenance)
        => _cells.TryGetValue(Key(sheetId, tableId, rowKey, columnId), out provenance);

    public IReadOnlyList<CellProvenance> Snapshot() => _cells.Values.OrderBy(x => x.SheetId).ThenBy(x => x.TableId).ThenBy(x => x.RowKey).ThenBy(x => x.ColumnId).ToArray();

    private static string Key(string sheet, string table, string row, string column)
        => $"{sheet}|{table}|{row}|{column}";
}

public sealed record FormulaDependencyEdge(string FromCell, string ToCell, string Relation);

public sealed class FormulaDependencyRuntime
{
    private readonly Dictionary<string, HashSet<string>> _dependencies = new(StringComparer.OrdinalIgnoreCase);

    public void Register(string cell, IEnumerable<string> dependencies)
    {
        if (string.IsNullOrWhiteSpace(cell)) throw new ArgumentException("Cell is required.", nameof(cell));
        _dependencies[cell] = dependencies.Where(x => !string.IsNullOrWhiteSpace(x)).ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<string> GetDependencies(string cell)
        => _dependencies.TryGetValue(cell, out var deps) ? deps.OrderBy(x => x).ToArray() : Array.Empty<string>();

    public IReadOnlyList<FormulaDependencyEdge> Edges() =>
        _dependencies.SelectMany(kvp => kvp.Value.Select(dep => new FormulaDependencyEdge(kvp.Key, dep, "FORMULA_REFERENCE"))).ToArray();
}
