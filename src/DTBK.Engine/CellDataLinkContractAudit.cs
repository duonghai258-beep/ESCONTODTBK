using System.Collections.Generic;
using System.Linq;

namespace DTBK.Engine;

public enum CellDataLinkStatus
{
    Complete,
    MissingProvider,
    MissingFormula,
    MissingDependency,
    MissingCalculationState,
    MissingOutputProjection,
    MissingGuiBinding,
    ContractOnly,
}

public sealed record CellDataLinkContract(
    string SheetId,
    string TableId,
    string ColumnId,
    string CellRole,
    string InputType,
    string? SourceType,
    string? SourceKey,
    string? ProviderOrParser,
    string? FormulaId,
    IReadOnlyList<string> DependencyIds,
    string? CalculationStatePath,
    string? OutputProjection,
    string? GuiControlId,
    string? EventId,
    string? CommandId,
    string? ValidationRuleId,
    string? ProvenanceId,
    bool RuntimeProviderWired = false,
    bool RuntimeFormulaWired = false,
    bool RuntimeCalculationStateWired = false,
    bool RuntimeGuiWired = false);

public sealed record CellDataLinkFinding(
    string SheetId,
    string TableId,
    string ColumnId,
    CellDataLinkStatus Status,
    string Message);

/// <summary>
/// Deterministic source-level audit for the 15-sheet cell data-link contract.
/// It never guesses missing wiring and never creates a second calculation path.
/// </summary>
public static class CellDataLinkContractAudit
{
    public static IReadOnlyList<CellDataLinkFinding> Audit(
        IEnumerable<CellDataLinkContract> contracts)
    {
        var findings = new List<CellDataLinkFinding>();

        foreach (var c in contracts)
        {
            if (c.CellRole is "Input" or "Lookup" && string.IsNullOrWhiteSpace(c.ProviderOrParser) &&
                string.IsNullOrWhiteSpace(c.SourceType))
            {
                findings.Add(new(c.SheetId, c.TableId, c.ColumnId,
                    CellDataLinkStatus.MissingProvider,
                    "Input/lookup has no concrete source type or provider/parser."));
                continue;
            }

            if (c.CellRole is "Calculated" or "Derived" or "Subtotal" or "Total" &&
                string.IsNullOrWhiteSpace(c.FormulaId))
            {
                findings.Add(new(c.SheetId, c.TableId, c.ColumnId,
                    CellDataLinkStatus.MissingFormula,
                    "Calculated cell has no formula ID."));
                continue;
            }

            if (c.CellRole is "Calculated" or "Derived" or "Subtotal" or "Total" &&
                c.DependencyIds.Count == 0)
            {
                findings.Add(new(c.SheetId, c.TableId, c.ColumnId,
                    CellDataLinkStatus.MissingDependency,
                    "Calculated cell has no declared dependency."));
                continue;
            }

            if (c.CellRole is "Calculated" or "Derived" or "Subtotal" or "Total" &&
                string.IsNullOrWhiteSpace(c.CalculationStatePath))
            {
                findings.Add(new(c.SheetId, c.TableId, c.ColumnId,
                    CellDataLinkStatus.MissingCalculationState,
                    "Calculated cell has no CalculationState path."));
                continue;
            }

            if (c.CellRole != "Header" && c.CellRole != "DisplayOnly" &&
                string.IsNullOrWhiteSpace(c.OutputProjection))
            {
                findings.Add(new(c.SheetId, c.TableId, c.ColumnId,
                    CellDataLinkStatus.MissingOutputProjection,
                    "Non-header cell has no output projection."));
                continue;
            }

            if (c.CellRole == "Input" && string.IsNullOrWhiteSpace(c.GuiControlId) &&
                string.IsNullOrWhiteSpace(c.SourceType))
            {
                findings.Add(new(c.SheetId, c.TableId, c.ColumnId,
                    CellDataLinkStatus.MissingGuiBinding,
                    "Input has neither GUI binding nor importer/source binding."));
                continue;
            }

            if (!c.RuntimeProviderWired && c.CellRole is "Input" or "Lookup")
            {
                findings.Add(new(c.SheetId, c.TableId, c.ColumnId,
                    CellDataLinkStatus.ContractOnly,
                    "Source/provider is declared but concrete runtime wiring is not proven."));
                continue;
            }

            if (!c.RuntimeFormulaWired && c.CellRole is "Calculated" or "Derived" or "Subtotal" or "Total")
            {
                findings.Add(new(c.SheetId, c.TableId, c.ColumnId,
                    CellDataLinkStatus.ContractOnly,
                    "Formula is declared but concrete runtime formula wiring is not proven."));
                continue;
            }

            if (!c.RuntimeCalculationStateWired && c.CellRole is "Calculated" or "Derived" or "Subtotal" or "Total")
            {
                findings.Add(new(c.SheetId, c.TableId, c.ColumnId,
                    CellDataLinkStatus.MissingCalculationState,
                    "CalculationState runtime path is not proven."));
                continue;
            }

            if (!c.RuntimeGuiWired && !string.IsNullOrWhiteSpace(c.GuiControlId))
            {
                findings.Add(new(c.SheetId, c.TableId, c.ColumnId,
                    CellDataLinkStatus.MissingGuiBinding,
                    "GUI control is declared but runtime binding is not proven."));
                continue;
            }

            findings.Add(new(c.SheetId, c.TableId, c.ColumnId,
                CellDataLinkStatus.Complete,
                "All required contract fields and declared runtime wiring are present."));
        }

        return findings;
    }

    public static IReadOnlyList<CellDataLinkFinding> MissingOnly(
        IEnumerable<CellDataLinkContract> contracts) =>
        Audit(contracts).Where(x => x.Status != CellDataLinkStatus.Complete).ToArray();
}
