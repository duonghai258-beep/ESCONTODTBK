using System.Globalization;
using DTBK.Domain;
using DTBK.Policy;
using DTBK.Coefficient;

namespace DTBK.Engine;

public sealed class WorkbookCalculationComposition
{
    private readonly CalculationStateEngine _stateEngine;
    private readonly EstimationWorkbook _workbook;

    public WorkbookCalculationComposition(EstimationWorkbook workbook, INormProvider normProvider, ILaborPriceProvider laborPriceProvider, IResourcePriceProvider resourcePriceProvider, IRateResolver rateResolver, ICoefficientProvider? coefficientProvider = null, RoundingPolicy? roundingPolicy = null)
    {
        _workbook = workbook ?? throw new ArgumentNullException(nameof(workbook));
        ArgumentNullException.ThrowIfNull(normProvider); ArgumentNullException.ThrowIfNull(laborPriceProvider); ArgumentNullException.ThrowIfNull(resourcePriceProvider); ArgumentNullException.ThrowIfNull(rateResolver);
        var normEngine = new NormEngine(normProvider, laborPriceProvider, resourcePriceProvider);
        var normCost = new NormCostBridge(normEngine, roundingPolicy);
        var costEngine = new ProjectCostEngine(rateResolver, coefficientProvider);
        _stateEngine = new CalculationStateEngine(normCost, costEngine);
    }

    public CalculationState Calculate(CalculationContext context, WorkType workType, CalculationScenario scenario, LocationType location, int provinceId, string vatCategory = "STANDARD")
    {
        ArgumentNullException.ThrowIfNull(context);
        if (!long.TryParse(context.ProjectId, NumberStyles.Integer, CultureInfo.InvariantCulture, out var projectId)) throw new InvalidOperationException($"ProjectId '{context.ProjectId}' phải là số để map vào ProjectItem.ProjectID.");
        var items = ReadProjectItems(projectId);
        if (items.Count == 0) throw new InvalidOperationException("S01/BOQ không có dòng công tác để tính.");
        return _stateEngine.Calculate(context, items, workType, scenario, location, provinceId, vatCategory);
    }

    private IReadOnlyList<ProjectItem> ReadProjectItems(long projectId)
    {
        if (!_workbook.Sheets.TryGetValue("S01", out var sheet) || !sheet.Tables.TryGetValue("BOQ", out var table)) return Array.Empty<ProjectItem>();
        var result = new List<ProjectItem>(); var sort = 0;
        foreach (var pair in table.Rows)
        {
            var row = pair.Value; var workCode = Read(row, "WorkCode"); if (string.IsNullOrWhiteSpace(workCode)) continue;
            var description = Read(row, "Description"); var unit = Read(row, "Unit"); var quantity = ReadDecimal(row, "Quantity"); var normCode = Read(row, "NormCode"); var itemId = ReadLong(row, "ItemId") ?? StableItemId(projectId, pair.Key);
            result.Add(new ProjectItem(itemId, projectId, workCode, description, unit, quantity, normCode, sort++));
        }
        return result;
    }
    private static string Read(IReadOnlyDictionary<string, WorkbookCell> row, string column) => row.TryGetValue(column, out var cell) ? cell.NormalizedValue ?? cell.RawValue ?? string.Empty : string.Empty;
    private static decimal ReadDecimal(IReadOnlyDictionary<string, WorkbookCell> row, string column){var value=Read(row,column);return decimal.TryParse(value,NumberStyles.Number,CultureInfo.InvariantCulture,out var result)?result:decimal.TryParse(value,NumberStyles.Number,CultureInfo.CurrentCulture,out result)?result:0m;}
    private static long? ReadLong(IReadOnlyDictionary<string, WorkbookCell> row, string column){var value=Read(row,column);return long.TryParse(value,NumberStyles.Integer,CultureInfo.InvariantCulture,out var result)?result:null;}
    private static long StableItemId(long projectId,string rowKey){unchecked{long hash=17;hash=hash*31+projectId;foreach(var c in rowKey)hash=hash*31+c;return hash==long.MinValue?1:Math.Abs(hash);}}
}

public sealed class CalculationStateWorkbookProjection
{
    private static readonly string[] SheetIds={"S01","S02","S03","S04","S05","S06","S07","S08","S09","S10","S11","S12","S13","S14","S15"};
    public void Project(EstimationWorkbook workbook, CalculationState state)
    {
        ArgumentNullException.ThrowIfNull(workbook); ArgumentNullException.ThrowIfNull(state);
        foreach(var sheetId in SheetIds){var sheet=workbook.GetOrCreateSheet(sheetId,sheetId);var table=sheet.GetOrCreateTable("CALCULATION_STATE");Write(table,"Summary","CalculationCode",state.Trace.CalculationCode,"CalculationState.Trace.CalculationCode");Write(table,"Summary","VerificationStatus",state.VerificationStatus.ToString(),"CalculationState.VerificationStatus");Write(table,"Summary","DirectCost",state.Summary.DirectCost,"CalculationState.Summary.DirectCost");Write(table,"Summary","TotalConstructionCost",state.Summary.TotalConstructionCost,"CalculationState.Summary.TotalConstructionCost");Write(table,"Summary","RevisionSource",state.Trace.CalculationCode,"CalculationState.Trace.CalculationCode");}
        var boq=workbook.GetOrCreateSheet("S01","Khối lượng").GetOrCreateTable("BOQ_CALCULATED");
        foreach(var line in state.Lines){var rowKey=line.Work.ProjectItemID.ToString(CultureInfo.InvariantCulture);Write(boq,rowKey,"Material",line.MaterialAmount,"CalculationState.Lines.MaterialAmount");Write(boq,rowKey,"Labor",line.LaborAmount,"CalculationState.Lines.LaborAmount");Write(boq,rowKey,"Machine",line.MachineAmount,"CalculationState.Lines.MachineAmount");Write(boq,rowKey,"Transport",line.TransportAmount,"CalculationState.Lines.TransportAmount");Write(boq,rowKey,"DirectAmount",line.DirectAmount,"CalculationState.Lines.DirectAmount");Write(boq,rowKey,"UnitPrice",line.UnitPrice,"CalculationState.Lines.UnitPrice");}
        var analysis=workbook.GetOrCreateSheet("S06","Phân tích").GetOrCreateTable("RESOURCE_REQUIREMENTS");var resources=workbook.GetOrCreateSheet("S07","Vật tư").GetOrCreateTable("RESOURCE_PRICES");
        foreach(var resource in state.Resources){var key=$"{resource.Type}:{resource.ResourceCode}:{resource.Unit}";Write(analysis,key,"ResourceType",resource.Type.ToString(),"CalculationState.Resources[].Type");Write(analysis,key,"ResourceCode",resource.ResourceCode,"CalculationState.Resources[].ResourceCode");Write(analysis,key,"ResourceName",resource.ResourceName,"CalculationState.Resources[].ResourceName");Write(analysis,key,"Unit",resource.Unit,"CalculationState.Resources[].Unit");Write(analysis,key,"Quantity",resource.Quantity,"CalculationState.Resources[].Quantity");Write(analysis,key,"UnitPrice",resource.UnitPrice,"CalculationState.Resources[].UnitPrice");Write(analysis,key,"Amount",resource.Amount,"CalculationState.Resources[].Amount");Write(analysis,key,"LaborGroup",resource.LaborGroup,"CalculationState.Resources[].LaborGroup");Write(analysis,key,"SourceNormDetail",resource.SourceNormDetail,"CalculationState.Resources[].SourceNormDetail");Write(resources,key,"ResourceType",resource.Type.ToString(),"CalculationState.Resources[].Type");Write(resources,key,"ResourceCode",resource.ResourceCode,"CalculationState.Resources[].ResourceCode");Write(resources,key,"ResourceName",resource.ResourceName,"CalculationState.Resources[].ResourceName");Write(resources,key,"Unit",resource.Unit,"CalculationState.Resources[].Unit");Write(resources,key,"UnitPrice",resource.UnitPrice,"CalculationState.Resources[].UnitPrice");Write(resources,key,"SourceNormDetail",resource.SourceNormDetail,"CalculationState.Resources[].SourceNormDetail");}
    }
    private static void Write(WorkbookTable table,string rowKey,string column,object? value,string domainPath){var cell=table.GetOrCreateCell(rowKey,column);cell.DomainPath=domainPath;cell.SetValue(Convert.ToString(value,CultureInfo.InvariantCulture));cell.MarkClean();}
}
