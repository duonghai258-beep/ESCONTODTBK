using System.Collections.ObjectModel;

namespace DTBK.Engine;

/// <summary>
/// Data-driven catalogue for the ESCON formula surface. The catalogue is intentionally
/// independent from the Avalonia UI so formula names can later come from workbook metadata
/// without changing presentation code.
/// </summary>
public sealed record EsconFunctionDefinition(
    string Name,
    int MinArguments,
    int? MaxArguments,
    string Description);

public sealed class EsconFormulaCatalog
{
    public static EsconFormulaCatalog Default { get; } = CreateDefault();

    public EsconFormulaCatalog(
        IEnumerable<EsconFunctionDefinition> functions,
        IEnumerable<string>? fieldNames = null)
    {
        Functions = new ReadOnlyCollection<EsconFunctionDefinition>(
            functions.Where(x => !string.IsNullOrWhiteSpace(x.Name))
                .GroupBy(x => x.Name.Trim(), StringComparer.OrdinalIgnoreCase)
                .Select(x => x.First())
                .OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
                .ToList());
        FieldNames = new ReadOnlyCollection<string>(
            (fieldNames ?? Array.Empty<string>())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim().Trim('[', ']'))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                .ToList());
    }

    public IReadOnlyList<EsconFunctionDefinition> Functions { get; }
    public IReadOnlyList<string> FieldNames { get; }

    public bool TryGetFunction(string name, out EsconFunctionDefinition definition)
    {
        definition = Functions.FirstOrDefault(x =>
            x.Name.Equals(name, StringComparison.OrdinalIgnoreCase))!;
        return definition is not null;
    }

    private static EsconFormulaCatalog CreateDefault() => new(
        new[]
        {
            new EsconFunctionDefinition("SPVALUE", 1, 1, "Resolve a system/context value."),
            new EsconFunctionDefinition("SUMGROUP", 1, 1, "Aggregate a column within the current group."),
            new EsconFunctionDefinition("PRODUCT", 1, null, "Multiply function arguments."),
            new EsconFunctionDefinition("SUM", 1, null, "Sum function arguments."),
            new EsconFunctionDefinition("IF", 3, 3, "Conditional expression."),
        });
}
