using System.Text.Json;

namespace DTBK.Domain;

public sealed record WorkbookSnapshotEnvelope(int SchemaVersion, long WorkbookRevision, string? ProjectId, string? DataPackVersion, IReadOnlyList<WorkbookSnapshotCell> Cells);
public sealed record WorkbookSnapshotCell(string SheetId, string TableId, string RowKey, string ColumnId, WorkbookCellType Type, WorkbookCellRole Role, WorkbookCellState State, string? RawValue, string? NormalizedValue, string? CalculatedValue, string? DomainPath, string? Formula, DataSourceRef? Source, string? ProviderId, IReadOnlyList<CellDependency> Dependencies, long Revision);

public sealed class WorkbookSnapshotStore
{
    private readonly JsonSerializerOptions _options = new(JsonSerializerDefaults.General) { WriteIndented = true };

    public string Serialize(EstimationWorkbook workbook, string? projectId = null, string? dataPackVersion = null)
    {
        ArgumentNullException.ThrowIfNull(workbook);
        var cells = workbook.Sheets.Values.SelectMany(s => s.Tables.Values).SelectMany(t => t.Rows.Values.SelectMany(r => r.Values))
            .OrderBy(c => c.Address, StringComparer.OrdinalIgnoreCase)
            .Select(c => new WorkbookSnapshotCell(c.SheetId, c.TableId, c.RowKey, c.ColumnId, c.Type, c.Role, c.State, c.RawValue, c.NormalizedValue, c.CalculatedValue, c.DomainPath, c.Formula, c.Source, c.ProviderId, c.Dependencies, c.Revision)).ToArray();
        return JsonSerializer.Serialize(new WorkbookSnapshotEnvelope(1, workbook.Revision, projectId, dataPackVersion, cells), _options);
    }

    public void Restore(EstimationWorkbook workbook, string json)
    {
        ArgumentNullException.ThrowIfNull(workbook);
        var snapshot = JsonSerializer.Deserialize<WorkbookSnapshotEnvelope>(json, _options) ?? throw new InvalidOperationException("Invalid workbook snapshot.");
        if (snapshot.SchemaVersion != 1) throw new InvalidOperationException($"Unsupported workbook snapshot schema {snapshot.SchemaVersion}.");
        foreach (var item in snapshot.Cells)
        {
            var cell = workbook.GetCell(item.SheetId, item.TableId, item.RowKey, item.ColumnId);
            if (item.Formula is null) cell.SetValue(item.RawValue, item.NormalizedValue); else cell.SetFormula(item.Formula, item.Dependencies);
            if (item.CalculatedValue is not null) cell.SetCalculatedValue(item.CalculatedValue);
            cell.DomainPath = item.DomainPath; cell.Source = item.Source; cell.ProviderId = item.ProviderId;
            if (item.Role == WorkbookCellRole.Override && item.Formula is null) cell.SetOverride(item.RawValue, item.NormalizedValue);
            if (item.State == WorkbookCellState.Invalid) cell.MarkInvalid(); else if (item.State == WorkbookCellState.Clean) cell.MarkClean(); else if (item.State == WorkbookCellState.Locked) cell.Lock();
        }
    }
}
