using System.Globalization;
using DTBK.Domain;

namespace DTBK.Engine;

public interface ICalculationSheetProjectionWriter
{
    string SheetId { get; }
    void Write(EstimationWorkbook workbook, CalculationState state);
}

/// <summary>
/// Materializes the canonical CalculationState into every standard sheet without
/// creating a second calculation engine. Domain-specific tables consume state fields;
/// unavailable fields are intentionally not invented.
/// </summary>
public sealed class Canonical15SheetProjectionWriters
{
    private readonly IReadOnlyList<ICalculationSheetProjectionWriter> _writers;

    public Canonical15SheetProjectionWriters()
    {
        _writers = new ICalculationSheetProjectionWriter[]
        {
            new S01ProjectionWriter(), new S02ProjectionWriter(), new S03ProjectionWriter(),
            new S04ProjectionWriter(), new S05ProjectionWriter(), new S06ProjectionWriter(),
            new S07ProjectionWriter(), new S08ProjectionWriter(), new S09ProjectionWriter(),
            new S10ProjectionWriter(), new S11ProjectionWriter(), new S12ProjectionWriter(),
            new S13ProjectionWriter(), new S14ProjectionWriter(), new S15ProjectionWriter()
        };
    }

    public void WriteAll(EstimationWorkbook workbook, CalculationState state)
    {
        ArgumentNullException.ThrowIfNull(workbook);
        ArgumentNullException.ThrowIfNull(state);
        foreach (var writer in _writers) writer.Write(workbook, state);
    }

    private abstract class BaseWriter : ICalculationSheetProjectionWriter
    {
        public abstract string SheetId { get; }

        public virtual void Write(EstimationWorkbook workbook, CalculationState state)
        {
            var table = workbook.GetOrCreateSheet(SheetId, SheetId).GetOrCreateTable("CALCULATION_STATE");
            Write(table, "Summary", "CalculationCode", state.Trace.CalculationCode, "CalculationState.Trace.CalculationCode");
            Write(table, "Summary", "VerificationStatus", state.VerificationStatus.ToString(), "CalculationState.VerificationStatus");
            Write(table, "Summary", "DirectCost", state.Summary.DirectCost, "CalculationState.Summary.DirectCost");
            Write(table, "Summary", "TotalConstructionCost", state.Summary.TotalConstructionCost, "CalculationState.Summary.TotalConstructionCost");
        }

        protected static void Write(WorkbookTable table, string rowKey, string column, object? value, string path)
        {
            var cell = table.GetOrCreateCell(rowKey, column);
            cell.DomainPath = path;
            cell.SetValue(Convert.ToString(value, CultureInfo.InvariantCulture));
            cell.MarkClean();
        }
    }

    private sealed class S01ProjectionWriter : BaseWriter { public override string SheetId => "S01"; public override void Write(EstimationWorkbook w, CalculationState s) { base.Write(w, s); var t=w.GetOrCreateSheet("S01","Khối lượng").GetOrCreateTable("BOQ_CALCULATED"); foreach(var l in s.Lines){var k=l.Work.ProjectItemID.ToString(CultureInfo.InvariantCulture); Write(t,k,"Material",l.MaterialAmount,"CalculationState.Lines.MaterialAmount"); Write(t,k,"Labor",l.LaborAmount,"CalculationState.Lines.LaborAmount"); Write(t,k,"Machine",l.MachineAmount,"CalculationState.Lines.MachineAmount"); Write(t,k,"Transport",l.TransportAmount,"CalculationState.Lines.TransportAmount"); Write(t,k,"DirectAmount",l.DirectAmount,"CalculationState.Lines.DirectAmount"); Write(t,k,"UnitPrice",l.UnitPrice,"CalculationState.Lines.UnitPrice");}} }
    private sealed class S02ProjectionWriter : BaseWriter { public override string SheetId => "S02"; }
    private sealed class S03ProjectionWriter : BaseWriter { public override string SheetId => "S03"; }
    private sealed class S04ProjectionWriter : BaseWriter { public override string SheetId => "S04"; }
    private sealed class S05ProjectionWriter : BaseWriter { public override string SheetId => "S05"; }
    private sealed class S06ProjectionWriter : BaseWriter { public override string SheetId => "S06"; public override void Write(EstimationWorkbook w, CalculationState s) { base.Write(w,s); var t=w.GetOrCreateSheet("S06","Phân tích").GetOrCreateTable("RESOURCE_REQUIREMENTS"); foreach(var r in s.Resources){var k=$"{r.Type}:{r.ResourceCode}:{r.Unit}"; Write(t,k,"ResourceType",r.Type,"CalculationState.Resources[].Type"); Write(t,k,"ResourceCode",r.ResourceCode,"CalculationState.Resources[].ResourceCode"); Write(t,k,"ResourceName",r.ResourceName,"CalculationState.Resources[].ResourceName"); Write(t,k,"Unit",r.Unit,"CalculationState.Resources[].Unit"); Write(t,k,"Quantity",r.Quantity,"CalculationState.Resources[].Quantity"); Write(t,k,"UnitPrice",r.UnitPrice,"CalculationState.Resources[].UnitPrice"); Write(t,k,"Amount",r.Amount,"CalculationState.Resources[].Amount");}} }
    private sealed class S07ProjectionWriter : BaseWriter { public override string SheetId => "S07"; public override void Write(EstimationWorkbook w, CalculationState s) { base.Write(w,s); var t=w.GetOrCreateSheet("S07","Vật tư").GetOrCreateTable("RESOURCE_PRICES"); foreach(var r in s.Resources){var k=$"{r.Type}:{r.ResourceCode}:{r.Unit}"; Write(t,k,"ResourceCode",r.ResourceCode,"CalculationState.Resources[].ResourceCode"); Write(t,k,"Unit",r.Unit,"CalculationState.Resources[].Unit"); Write(t,k,"UnitPrice",r.UnitPrice,"CalculationState.Resources[].UnitPrice");}} }
    private sealed class S08ProjectionWriter : BaseWriter { public override string SheetId => "S08"; }
    private sealed class S09ProjectionWriter : BaseWriter { public override string SheetId => "S09"; }
    private sealed class S10ProjectionWriter : BaseWriter { public override string SheetId => "S10"; }
    private sealed class S11ProjectionWriter : BaseWriter { public override string SheetId => "S11"; }
    private sealed class S12ProjectionWriter : BaseWriter { public override string SheetId => "S12"; }
    private sealed class S13ProjectionWriter : BaseWriter { public override string SheetId => "S13"; }
    private sealed class S14ProjectionWriter : BaseWriter { public override string SheetId => "S14"; }
    private sealed class S15ProjectionWriter : BaseWriter { public override string SheetId => "S15"; }
}
