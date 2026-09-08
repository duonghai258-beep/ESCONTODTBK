using System.Collections.ObjectModel;

namespace DTBK.Domain;

public enum WorkbookCellType { Text, Number, Date, Boolean, Formula, Error, Empty }
public enum WorkbookCellState { Clean, Dirty, Invalid, Locked }
public enum WorkbookCellRole { Input, Calculated, Lookup, Linked, ReadOnly, Override }

public sealed record CellDependency(string SheetId, string TableId, string RowKey, string ColumnId);

public sealed class WorkbookCell
{
    public required string SheetId { get; init; }
    public required string TableId { get; init; }
    public required string RowKey { get; init; }
    public required string ColumnId { get; init; }
    public WorkbookCellType Type { get; set; }
    public WorkbookCellState State { get; private set; } = WorkbookCellState.Clean;
    public WorkbookCellRole Role { get; set; } = WorkbookCellRole.Input;
    public string? RawValue { get; private set; }
    public string? NormalizedValue { get; private set; }
    public string? CalculatedValue { get; private set; }
    public string? DomainPath { get; set; }
    public string? Formula { get; private set; }
    public DataSourceRef? Source { get; set; }
    public string? ProviderId { get; set; }
    public long Revision { get; private set; }
    public IReadOnlyList<CellDependency> Dependencies => new ReadOnlyCollection<CellDependency>(_dependencies);
    private readonly List<CellDependency> _dependencies = new();

    public string Address => $"{SheetId}!{TableId}:{RowKey}/{ColumnId}";

    public void SetValue(string? rawValue, string? normalizedValue = null)
    {
        EnsureWritable();
        RawValue = rawValue;
        NormalizedValue = normalizedValue ?? rawValue?.Trim();
        CalculatedValue = null;
        Formula = null;
        _dependencies.Clear();
        Type = InferType(NormalizedValue);
        State = WorkbookCellState.Dirty;
        Role = WorkbookCellRole.Input;
        Revision++;
    }

    public void SetCalculatedValue(string? value)
    {
        EnsureWritable();
        CalculatedValue = value;
        Type = WorkbookCellType.Formula;
        State = WorkbookCellState.Dirty;
        Revision++;
    }

    public void SetFormula(string formula, IEnumerable<CellDependency> dependencies)
    {
        EnsureWritable();
        Formula = string.IsNullOrWhiteSpace(formula) ? throw new ArgumentException("Formula is required.", nameof(formula)) : formula;
        _dependencies.Clear();
        _dependencies.AddRange(dependencies.Distinct());
        Type = WorkbookCellType.Formula;
        Role = WorkbookCellRole.Calculated;
        State = WorkbookCellState.Dirty;
        Revision++;
    }

    public void SetOverride(string? value, string? normalizedValue = null)
    {
        EnsureWritable();
        RawValue = value;
        NormalizedValue = normalizedValue ?? value?.Trim();
        CalculatedValue = null;
        Role = WorkbookCellRole.Override;
        State = WorkbookCellState.Dirty;
        Revision++;
    }

    public void Invalidate() { if (State != WorkbookCellState.Locked) { State = WorkbookCellState.Dirty; Revision++; } }
    public void MarkClean() { if (State != WorkbookCellState.Locked) State = WorkbookCellState.Clean; }
    public void MarkInvalid() { if (State != WorkbookCellState.Locked) State = WorkbookCellState.Invalid; }
    public void Lock() => State = WorkbookCellState.Locked;

    private void EnsureWritable()
    {
        if (State == WorkbookCellState.Locked) throw new InvalidOperationException($"Cell {Address} is locked.");
    }

    private static WorkbookCellType InferType(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return WorkbookCellType.Empty;
        if (decimal.TryParse(value, out _)) return WorkbookCellType.Number;
        if (DateOnly.TryParse(value, out _)) return WorkbookCellType.Date;
        if (bool.TryParse(value, out _)) return WorkbookCellType.Boolean;
        return WorkbookCellType.Text;
    }
}

public sealed class WorkbookTable
{
    private readonly Dictionary<string, Dictionary<string, WorkbookCell>> _rows = new(StringComparer.OrdinalIgnoreCase);
    public required string SheetId { get; init; }
    public required string TableId { get; init; }
    public IReadOnlyDictionary<string, Dictionary<string, WorkbookCell>> Rows => _rows;

    public WorkbookCell GetOrCreateCell(string rowKey, string columnId)
    {
        if (!_rows.TryGetValue(rowKey, out var row))
            _rows[rowKey] = row = new Dictionary<string, WorkbookCell>(StringComparer.OrdinalIgnoreCase);
        if (!row.TryGetValue(columnId, out var cell))
            row[columnId] = cell = new WorkbookCell { SheetId = SheetId, TableId = TableId, RowKey = rowKey, ColumnId = columnId, Type = WorkbookCellType.Empty };
        return cell;
    }
}

public sealed class WorkbookSheet
{
    private readonly Dictionary<string, WorkbookTable> _tables = new(StringComparer.OrdinalIgnoreCase);
    public required string SheetId { get; init; }
    public string? DisplayName { get; init; }
    public IReadOnlyDictionary<string, WorkbookTable> Tables => _tables;

    public WorkbookTable GetOrCreateTable(string tableId)
    {
        if (!_tables.TryGetValue(tableId, out var table))
            _tables[tableId] = table = new WorkbookTable { SheetId = SheetId, TableId = tableId };
        return table;
    }
}

public sealed class EstimationWorkbook
{
    private readonly Dictionary<string, WorkbookSheet> _sheets = new(StringComparer.OrdinalIgnoreCase);
    public IReadOnlyDictionary<string, WorkbookSheet> Sheets => _sheets;
    public long Revision { get; private set; }

    public WorkbookSheet GetOrCreateSheet(string sheetId, string? displayName = null)
    {
        if (!_sheets.TryGetValue(sheetId, out var sheet))
            _sheets[sheetId] = sheet = new WorkbookSheet { SheetId = sheetId, DisplayName = displayName };
        return sheet;
    }

    public WorkbookCell GetCell(string sheetId, string tableId, string rowKey, string columnId) =>
        GetOrCreateSheet(sheetId).GetOrCreateTable(tableId).GetOrCreateCell(rowKey, columnId);

    public void SetCell(string sheetId, string tableId, string rowKey, string columnId, string? value, string? domainPath = null, DataSourceRef? source = null)
    {
        var cell = GetCell(sheetId, tableId, rowKey, columnId);
        cell.DomainPath = domainPath;
        cell.Source = source;
        cell.SetValue(value);
        Revision++;
    }
}
