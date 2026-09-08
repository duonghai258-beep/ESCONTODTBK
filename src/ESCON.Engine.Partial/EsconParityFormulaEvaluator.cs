using System.Text.RegularExpressions;

namespace DTBK.Engine;

/// <summary>
/// Evidence-bounded adapter for ESCON formulas that mix numeric fields with
/// text selections and boolean helpers. Numeric evaluation remains delegated
/// to the canonical ESCON calculation engine.
/// </summary>
public static class EsconParityFormulaEvaluator
{
    private static readonly Regex TextEquality = new(
        "\\[(?<name>[^\\]]+)\\]\\s*(?<op>=|<>)\\s*\\\"(?<value>(?:\\\\.|[^\\\"])*)\\\"",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static EsconCalculationResult Evaluate(
        IReadOnlyDictionary<string, string> formulas,
        IReadOnlyDictionary<string, decimal> numericInputs,
        IReadOnlyDictionary<string, string> textInputs,
        Func<string, decimal>? spValue = null,
        Func<string, decimal>? sumGroup = null,
        IReadOnlyDictionary<string, decimal>? initialValues = null,
        EsconCalculationOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(formulas);
        ArgumentNullException.ThrowIfNull(numericInputs);
        ArgumentNullException.ThrowIfNull(textInputs);

        var normalized = formulas.ToDictionary(
            x => x.Key,
            x => Normalize(x.Value, textInputs),
            StringComparer.Ordinal);

        return new EsconCalculationAccuracyEngine(options).Evaluate(
            normalized, numericInputs, spValue, sumGroup, initialValues);
    }

    private static string Normalize(string formula, IReadOnlyDictionary<string, string> textInputs)
    {
        var result = TextEquality.Replace(formula, match =>
        {
            var name = match.Groups["name"].Value.Trim();
            var expected = Regex.Unescape(match.Groups["value"].Value);
            var actual = textInputs.TryGetValue(name, out var value) ? value : string.Empty;
            var equal = string.Equals(actual, expected, StringComparison.Ordinal);
            var truth = match.Groups["op"].Value == "=" ? equal : !equal;
            return truth ? "1" : "0";
        });

        result = ReplaceBooleanFunction(result, "OR", false);
        result = ReplaceBooleanFunction(result, "AND", true);
        return result;
    }

    private static string ReplaceBooleanFunction(string formula, string function, bool all)
    {
        var needle = function + "(";
        while (true)
        {
            var start = formula.LastIndexOf(needle, StringComparison.OrdinalIgnoreCase);
            if (start < 0) return formula;
            var close = FindMatchingClose(formula, start + needle.Length);
            if (close < 0) throw new FormatException($"Unclosed {function}(...).");
            var args = SplitArguments(formula[(start + needle.Length)..close]);
            if (args.Count == 0) throw new FormatException($"{function} requires at least one argument.");
            var replacement = all
                ? $"({string.Join("*", args.Select(x => $"(({x})<>0)"))})"
                : $"({string.Join("+", args.Select(x => $"(({x})<>0)"))})";
            formula = formula[..start] + replacement + formula[(close + 1)..];
        }
    }

    private static IReadOnlyList<string> SplitArguments(string value)
    {
        var result = new List<string>();
        var start = 0;
        var depth = 0;
        for (var i = 0; i < value.Length; i++)
        {
            if (value[i] == '(') depth++;
            else if (value[i] == ')') depth--;
            else if ((value[i] == ';' || value[i] == ',') && depth == 0)
            {
                var item = value[start..i].Trim();
                if (item.Length > 0) result.Add(item);
                start = i + 1;
            }
        }
        var tail = value[start..].Trim();
        if (tail.Length > 0) result.Add(tail);
        return result;
    }

    private static int FindMatchingClose(string formula, int openPosition)
    {
        var depth = 1;
        for (var i = openPosition; i < formula.Length; i++)
        {
            if (formula[i] == '(') depth++;
            else if (formula[i] == ')' && --depth == 0) return i;
        }
        return -1;
    }
}
