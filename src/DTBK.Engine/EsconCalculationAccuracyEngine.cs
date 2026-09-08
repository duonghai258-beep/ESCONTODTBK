using System.Globalization;
using System.Text.RegularExpressions;

namespace DTBK.Engine;

/// <summary>
/// Deterministic calculation core for the ESCON reconstruction path.
/// Keeps formula evaluation, dependency ordering, SPVALUE/SUMGROUP resolution,
/// intermediate precision and final rounding explicit and auditable.
/// </summary>
public sealed class EsconCalculationAccuracyEngine
{
    private static readonly Regex Reference = new(@"\[(?<name>[^\]]+)\]", RegexOptions.Compiled);
    private static readonly Regex FormulaFunction = new(@"(?i)\b(?:SPVALUE|SUMGROUP|PRODUCT|SUM|IF)\s*\(", RegexOptions.Compiled);

    public EsconCalculationAccuracyEngine(EsconCalculationOptions? options = null)
        => Options = options ?? EsconCalculationOptions.Default;

    public EsconCalculationOptions Options { get; }

    public EsconCalculationResult Evaluate(
        IReadOnlyDictionary<string, string> formulas,
        IReadOnlyDictionary<string, decimal> inputs,
        Func<string, decimal>? spValue = null,
        Func<string, decimal>? sumGroup = null,
        IReadOnlyDictionary<string, decimal>? initialValues = null)
    {
        ArgumentNullException.ThrowIfNull(formulas);
        ArgumentNullException.ThrowIfNull(inputs);

        var values = new Dictionary<string, decimal>(inputs, StringComparer.OrdinalIgnoreCase);
        if (initialValues is not null)
            foreach (var pair in initialValues) values[pair.Key] = pair.Value;

        var scheduler = new RecalculationScheduler();
        foreach (var formula in formulas)
            scheduler.AddFormulaDependencies(formula.Key, ExtractDependencies(formula.Value, formula.Key));

        var plan = scheduler.Plan(formulas.Keys);
        var trace = new List<EsconCalculationStep>();

        foreach (var target in plan.EvaluationOrder)
        {
            if (!formulas.TryGetValue(target, out var expression)) continue;
            var raw = new Parser(expression, name => Resolve(name, values),
                spValue ?? (_ => throw new InvalidOperationException("SPVALUE provider is required.")),
                sumGroup ?? (_ => throw new InvalidOperationException("SUMGROUP provider is required."))).Parse();
            var stored = Options.RoundIntermediateValues ? Round(raw) : raw;
            values[target] = stored;
            trace.Add(new EsconCalculationStep(target, expression, raw, stored));
        }

        return new EsconCalculationResult(values, trace, plan.EvaluationOrder);
    }

    private decimal Round(decimal value) => decimal.Round(value, Options.Scale, Options.RoundingMode);

    private static decimal Resolve(string name, IReadOnlyDictionary<string, decimal> values)
        => values.TryGetValue(name, out var value)
            ? value
            : throw new KeyNotFoundException($"Calculation variable '{name}' was not resolved.");

    private static IEnumerable<string> ExtractDependencies(string expression, string target)
    {
        foreach (Match match in Reference.Matches(expression))
        {
            var name = match.Groups["name"].Value.Trim();
            if (!name.Equals(target, StringComparison.OrdinalIgnoreCase)) yield return name;
        }

        foreach (Match match in Regex.Matches(expression, @"(?<![\w])([A-Za-z_][A-Za-z0-9_]*)\s*(?=\))"))
        {
            var name = match.Groups[1].Value;
            if (!FormulaFunction.IsMatch(name + "(") && !name.Equals(target, StringComparison.OrdinalIgnoreCase)) yield return name;
        }
    }

    private sealed class Parser
    {
        private readonly string _text;
        private readonly Func<string, decimal> _variable;
        private readonly Func<string, decimal> _spValue;
        private readonly Func<string, decimal> _sumGroup;
        private int _position;

        public Parser(string text, Func<string, decimal> variable, Func<string, decimal> spValue, Func<string, decimal> sumGroup)
        { _text = text; _variable = variable; _spValue = spValue; _sumGroup = sumGroup; }

        public decimal Parse()
        {
            var result = ParseComparison();
            Skip();
            if (_position != _text.Length) throw new FormatException($"Unexpected token at {_position}: '{_text[_position..]}'.");
            return result;
        }

        private decimal ParseComparison()
        {
            var left = ParseAdditive();
            Skip();
            while (true)
            {
                string? op = null;
                foreach (var candidate in new[] { ">=", "<=", "<>", "=", ">", "<" })
                    if (Take(candidate)) { op = candidate; break; }
                if (op is null) return left;
                var right = ParseAdditive();
                left = op switch
                {
                    "=" => left == right ? 1m : 0m, ">" => left > right ? 1m : 0m,
                    "<" => left < right ? 1m : 0m, ">=" => left >= right ? 1m : 0m,
                    "<=" => left <= right ? 1m : 0m, "<>" => left != right ? 1m : 0m,
                    _ => throw new InvalidOperationException()
                };
            }
        }

        private decimal ParseAdditive()
        {
            var value = ParseMultiplicative();
            while (true)
            {
                Skip();
                if (Take("+")) value += ParseMultiplicative();
                else if (Take("-")) value -= ParseMultiplicative();
                else return value;
            }
        }

        private decimal ParseMultiplicative()
        {
            var value = ParsePower();
            while (true)
            {
                Skip();
                if (Take("*")) value *= ParsePower();
                else if (Take("/"))
                {
                    var divisor = ParsePower();
                    if (divisor == 0m) throw new DivideByZeroException();
                    value /= divisor;
                }
                else return value;
            }
        }

        private decimal ParsePower()
        {
            var value = ParseUnary();
            Skip();
            if (Take("^"))
            {
                var exponent = ParsePower();
                var powered = Math.Pow((double)value, (double)exponent);
                if (double.IsNaN(powered) || double.IsInfinity(powered) || powered > (double)decimal.MaxValue || powered < (double)decimal.MinValue)
                    throw new OverflowException("ESCON power result is outside decimal range.");
                value = (decimal)powered;
            }
            return value;
        }

        private decimal ParseUnary()
        {
            Skip();
            if (Take("+")) return ParseUnary();
            if (Take("-")) return -ParseUnary();
            return ParsePrimary();
        }

        private decimal ParsePrimary()
        {
            Skip();
            if (Take("(")) { var v = ParseComparison(); Expect(")"); return v; }
            if (_position < _text.Length && _text[_position] == '[')
            {
                var end = _text.IndexOf(']', _position + 1);
                if (end < 0) throw new FormatException("Unclosed [reference].");
                var name = _text[(_position + 1)..end].Trim(); _position = end + 1; return _variable(name);
            }
            if (char.IsDigit(Current()) || Current() == '.') return ParseNumber();
            var identifier = ParseIdentifier();
            Skip();
            if (!Take("(")) return _variable(identifier);
            return ParseFunction(identifier);
        }

        private decimal ParseFunction(string name)
        {
            if (name.Equals("SPVALUE", StringComparison.OrdinalIgnoreCase))
            {
                var key = ParseNameArgument(); Expect(")"); return _spValue(key);
            }
            if (name.Equals("SUMGROUP", StringComparison.OrdinalIgnoreCase))
            {
                var arg = ReadRawUntilClose(); Expect(")"); return _sumGroup(NormalizeArgument(arg));
            }
            if (name.Equals("PRODUCT", StringComparison.OrdinalIgnoreCase) || name.Equals("SUM", StringComparison.OrdinalIgnoreCase))
            {
                var args = new List<decimal>();
                while (true)
                {
                    args.Add(ParseComparison()); Skip();
                    if (Take(")")) break;
                    if (!Take(",") && !Take(";")) throw new FormatException("Expected function separator.");
                }
                return name.Equals("PRODUCT", StringComparison.OrdinalIgnoreCase) ? args.Aggregate(1m, (a, b) => a * b) : args.Sum();
            }
            if (name.Equals("IF", StringComparison.OrdinalIgnoreCase))
            {
                var condition = ParseComparison();
                ExpectAnySeparator();
                var trueExpression = ReadArgumentUntilTopLevelDelimiter();
                ExpectAnySeparator();
                var falseExpression = ReadArgumentUntilTopLevelClose();
                Expect(")");
                var selected = condition != 0m ? trueExpression : falseExpression;
                return new Parser(selected, _variable, _spValue, _sumGroup).Parse();
            }
            throw new NotSupportedException($"Unsupported formula function '{name}'.");
        }

        private string ParseNameArgument()
        {
            Skip();
            if (_position < _text.Length && _text[_position] == '[')
            {
                var end = _text.IndexOf(']', _position + 1); if (end < 0) throw new FormatException("Unclosed argument.");
                var value = _text[(_position + 1)..end].Trim(); _position = end + 1; return value;
            }
            return ParseIdentifier();
        }

        private string ReadRawUntilClose()
        {
            var start = _position; var depth = 0;
            while (_position < _text.Length)
            {
                var c = _text[_position++];
                if (c == '(') depth++;
                else if (c == ')') { if (depth == 0) { _position--; break; } depth--; }
            }
            return _text[start.._position].Trim();
        }

        private string ReadArgumentUntilTopLevelDelimiter()
        {
            var start = _position; var depth = 0;
            while (_position < _text.Length)
            {
                var c = _text[_position];
                if (c == '(') depth++;
                else if (c == ')')
                {
                    if (depth == 0) throw new FormatException("Expected function separator before closing parenthesis.");
                    depth--;
                }
                else if ((c == ',' || c == ';') && depth == 0) break;
                _position++;
            }
            return _text[start.._position].Trim();
        }

        private string ReadArgumentUntilTopLevelClose()
        {
            var start = _position; var depth = 0;
            while (_position < _text.Length)
            {
                var c = _text[_position];
                if (c == '(') depth++;
                else if (c == ')')
                {
                    if (depth == 0) break;
                    depth--;
                }
                _position++;
            }
            return _text[start.._position].Trim();
        }

        private static string NormalizeArgument(string value) => value.Trim().TrimStart('[').TrimEnd(']').Trim();
        private decimal ParseNumber()
        {
            var start = _position;
            var hasDot = false;
            while (_position < _text.Length)
            {
                var c = _text[_position];
                if (char.IsDigit(c)) { _position++; continue; }
                if (c == '.' && !hasDot) { hasDot = true; _position++; continue; }
                break;
            }
            return decimal.Parse(_text[start.._position], CultureInfo.InvariantCulture);
        }
        private string ParseIdentifier() { var start = _position; while (_position < _text.Length && (char.IsLetterOrDigit(_text[_position]) || _text[_position] == '_')) _position++; if (start == _position) throw new FormatException($"Identifier expected at {_position}."); return _text[start.._position]; }
        private void Expect(string token) { Skip(); if (!Take(token)) throw new FormatException($"Expected '{token}' at {_position}."); }
        private void ExpectAnySeparator() { Skip(); if (!Take(",") && !Take(";")) throw new FormatException("Expected function separator."); }
        private bool Take(string token) { Skip(); if (_text.AsSpan(_position).StartsWith(token.AsSpan(), StringComparison.Ordinal)) { _position += token.Length; return true; } return false; }
        private void Skip() { while (_position < _text.Length && char.IsWhiteSpace(_text[_position])) _position++; }
        private char Current() => _position < _text.Length ? _text[_position] : '\0';
    }
}

public sealed record EsconCalculationOptions(int Scale, MidpointRounding RoundingMode, bool RoundIntermediateValues)
{
    public static EsconCalculationOptions Default { get; } = new(6, MidpointRounding.AwayFromZero, false);
}

public sealed record EsconCalculationStep(string Target, string Formula, decimal RawValue, decimal StoredValue);

public sealed record EsconCalculationResult(
    IReadOnlyDictionary<string, decimal> Values,
    IReadOnlyList<EsconCalculationStep> Trace,
    IReadOnlyList<string> EvaluationOrder);
