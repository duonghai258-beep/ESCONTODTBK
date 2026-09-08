using System.Globalization;
using DTBK.Domain;

namespace DTBK.Engine;

public interface IFormulaContext
{
    decimal? Field(string name);
    decimal? SpValue(string name);
    decimal SumGroup(string field);
}

public sealed record FormulaEvaluationResult(decimal Value, string Expression, IReadOnlyList<string> Dependencies);

/// <summary>
/// Canonical formula evaluator for workbook formulas. The evaluator is deliberately
/// independent from the GUI and only resolves formula semantics/dependencies; cost
/// calculation remains owned by the canonical calculation engines.
/// </summary>
public sealed class CanonicalFormulaEngine
{
    public FormulaEvaluationResult Evaluate(string expression, IFormulaContext context)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(expression);
        ArgumentNullException.ThrowIfNull(context);
        var deps = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var parser = new Parser(expression, context, deps);
        var value = parser.ParseExpression();
        parser.ExpectEnd();
        return new FormulaEvaluationResult(value, expression, deps.ToArray());
    }

    private sealed class Parser
    {
        private readonly string _s;
        private readonly IFormulaContext _ctx;
        private readonly HashSet<string> _deps;
        private int _p;

        public Parser(string s, IFormulaContext ctx, HashSet<string> deps) { _s = s; _ctx = ctx; _deps = deps; }
        public decimal ParseExpression() => ParseComparison();

        private decimal ParseComparison()
        {
            var left = ParseAdditive();
            Skip();
            if (Match("<=")) return Bool(ParseAdditive() >= left);
            if (Match(">=")) return Bool(ParseAdditive() <= left);
            if (Match("<>")) return Bool(ParseAdditive() != left);
            if (Match("=")) return Bool(left == ParseAdditive());
            if (Match("<")) return Bool(left < ParseAdditive());
            if (Match(">")) return Bool(left > ParseAdditive());
            return left;
        }

        private decimal ParseAdditive()
        {
            var v = ParseMultiplicative();
            while (true)
            {
                Skip();
                if (Match("+")) v += ParseMultiplicative();
                else if (Match("-")) v -= ParseMultiplicative();
                else return v;
            }
        }

        private decimal ParseMultiplicative()
        {
            var v = ParseUnary();
            while (true)
            {
                Skip();
                if (Match("*")) v *= ParseUnary();
                else if (Match("/"))
                {
                    var d = ParseUnary();
                    if (d == 0m) throw new DivideByZeroException("Formula division by zero.");
                    v /= d;
                }
                else return v;
            }
        }

        private decimal ParseUnary()
        {
            Skip();
            if (Match("+")) return ParseUnary();
            if (Match("-")) return -ParseUnary();
            if (Match("(")) { var v = ParseExpression(); Expect(")"); return v; }
            if (Peek('[')) return ParseField();
            if (char.IsLetter(PeekChar())) return ParseIdentifierOrFunction();
            return ParseNumber();
        }

        private decimal ParseField()
        {
            Expect("[");
            var start = _p;
            while (_p < _s.Length && _s[_p] != ']') _p++;
            if (_p >= _s.Length) throw Error("Unclosed field reference.");
            var name = _s[start.._p];
            _p++;
            _deps.Add("FIELD:" + name);
            return _ctx.Field(name) ?? 0m;
        }

        private decimal ParseIdentifierOrFunction()
        {
            var start = _p;
            while (_p < _s.Length && (char.IsLetterOrDigit(_s[_p]) || _s[_p] == '_')) _p++;
            var name = _s[start.._p];
            Skip();
            if (!Match("(")) throw Error($"Unknown identifier '{name}'.");
            var args = new List<object>();
            while (true)
            {
                Skip();
                if (Match(")")) break;
                if (Peek('"')) args.Add(ParseString());
                else if (Peek('[') && (name.Equals("SPVALUE", StringComparison.OrdinalIgnoreCase) || name.Equals("SUMGROUP", StringComparison.OrdinalIgnoreCase))) args.Add(ParseFieldNameOnly());
                else args.Add(ParseExpression());
                Skip();
                if (Match(")")) break;
                Expect(";");
            }

            if (name.Equals("IF", StringComparison.OrdinalIgnoreCase))
            {
                if (args.Count != 3) throw Error("IF requires 3 arguments.");
                return ToDecimal(args[0]) != 0m ? ToDecimal(args[1]) : ToDecimal(args[2]);
            }
            if (name.Equals("OR", StringComparison.OrdinalIgnoreCase)) return args.Any(x => ToDecimal(x) != 0m) ? 1m : 0m;
            if (name.Equals("PRODUCT", StringComparison.OrdinalIgnoreCase))
            {
                decimal r = 1m;
                foreach (var a in args) r *= ToDecimal(a);
                return r;
            }
            if (name.Equals("SPVALUE", StringComparison.OrdinalIgnoreCase))
            {
                if (args.Count != 1) throw Error("SPVALUE requires 1 argument.");
                var key = ToStringValue(args[0]);
                _deps.Add("SPVALUE:" + key);
                return _ctx.SpValue(key) ?? 0m;
            }
            if (name.Equals("SUMGROUP", StringComparison.OrdinalIgnoreCase))
            {
                if (args.Count != 1) throw Error("SUMGROUP requires 1 argument.");
                var field = ToStringValue(args[0]);
                _deps.Add("SUMGROUP:" + field);
                return _ctx.SumGroup(field);
            }
            throw Error($"Unsupported formula function '{name}'.");
        }

        private string ParseFieldNameOnly()
        {
            Expect("[");
            var start = _p;
            while (_p < _s.Length && _s[_p] != ']') _p++;
            if (_p >= _s.Length) throw Error("Unclosed field argument.");
            var n = _s[start.._p];
            _p++;
            _deps.Add("FIELD:" + n);
            return n;
        }

        private string ParseString()
        {
            Expect("\"");
            var start = _p;
            while (_p < _s.Length && _s[_p] != '"') _p++;
            if (_p >= _s.Length) throw Error("Unclosed string literal.");
            var v = _s[start.._p];
            _p++;
            return v;
        }

        private decimal ParseNumber()
        {
            Skip();
            var start = _p;
            while (_p < _s.Length && (char.IsDigit(_s[_p]) || _s[_p] == '.')) _p++;
            if (start == _p) throw Error("Expected number or expression.");
            return decimal.Parse(_s[start.._p], CultureInfo.InvariantCulture);
        }

        private static decimal ToDecimal(object x) => x is decimal d ? d : decimal.Parse(Convert.ToString(x, CultureInfo.InvariantCulture) ?? "0", CultureInfo.InvariantCulture);
        private static string ToStringValue(object x) => x?.ToString() ?? "";
        private static decimal Bool(bool x) => x ? 1m : 0m;
        private char PeekChar() => _p < _s.Length ? _s[_p] : '\0';
        private bool Peek(char c) { Skip(); return PeekChar() == c; }
        private bool Match(string token)
        {
            Skip();
            if (_s.AsSpan(_p).StartsWith(token, StringComparison.OrdinalIgnoreCase)) { _p += token.Length; return true; }
            return false;
        }
        private void Expect(string token) { if (!Match(token)) throw Error($"Expected '{token}'."); }
        public void ExpectEnd() { Skip(); if (_p != _s.Length) throw Error("Unexpected trailing formula text."); }
        private void Skip() { while (_p < _s.Length && char.IsWhiteSpace(_s[_p])) _p++; }
        private Exception Error(string message) => new FormatException($"{message} Position {_p} in '{_s}'.");
    }
}

public sealed class WorkbookFormulaCoordinator
{
    private readonly EstimationWorkbook _workbook;
    private readonly CanonicalFormulaEngine _formulaEngine;
    private readonly RecalculationScheduler _scheduler;

    public WorkbookFormulaCoordinator(EstimationWorkbook workbook, CanonicalFormulaEngine? formulaEngine = null, RecalculationScheduler? scheduler = null)
    {
        _workbook = workbook ?? throw new ArgumentNullException(nameof(workbook));
        _formulaEngine = formulaEngine ?? new CanonicalFormulaEngine();
        _scheduler = scheduler ?? new RecalculationScheduler();
    }

    public RecalculationScheduler Scheduler => _scheduler;

    public void RegisterFormula(string sheetId, string tableId, string rowKey, string columnId, string formula, IEnumerable<string> dependencyAddresses)
    {
        var dependencies = dependencyAddresses.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        var cell = _workbook.GetCell(sheetId, tableId, rowKey, columnId);
        cell.SetFormula(formula, dependencies.Select(ParseDependency));
        foreach (var dependency in dependencies) _scheduler.AddDependency(dependency, cell.Address, "FORMULA");
    }

    public FormulaEvaluationResult RecalculateCell(string sheetId, string tableId, string rowKey, string columnId, IFormulaContext context)
    {
        var cell = _workbook.GetCell(sheetId, tableId, rowKey, columnId);
        if (string.IsNullOrWhiteSpace(cell.Formula)) throw new InvalidOperationException($"Cell {cell.Address} has no formula.");
        var result = _formulaEngine.Evaluate(cell.Formula, context);
        cell.SetCalculatedValue(result.Value.ToString(CultureInfo.InvariantCulture));
        cell.MarkClean();
        return result;
    }

    public RecalculationPlan Plan(IEnumerable<string> changedAddresses) => _scheduler.Plan(changedAddresses);

    public IReadOnlyList<string> RecalculateAffected(IEnumerable<string> changedAddresses, IFormulaContext context, Action? canonicalCalculation = null)
    {
        var plan = _scheduler.Plan(changedAddresses);
        canonicalCalculation?.Invoke();
        var recalculated = new List<string>();
        foreach (var address in plan.EvaluationOrder)
        {
            var cell = FindCell(address);
            if (cell is null || string.IsNullOrWhiteSpace(cell.Formula)) continue;
            RecalculateCell(cell.SheetId, cell.TableId, cell.RowKey, cell.ColumnId, context);
            recalculated.Add(address);
        }
        return recalculated;
    }

    private WorkbookCell? FindCell(string address)
    {
        var dependency = ParseDependency(address);
        if (!_workbook.Sheets.TryGetValue(dependency.SheetId, out var sheet)) return null;
        if (!sheet.Tables.TryGetValue(dependency.TableId, out var table)) return null;
        return table.Rows.TryGetValue(dependency.RowKey, out var row) && row.TryGetValue(dependency.ColumnId, out var cell) ? cell : null;
    }

    private static CellDependency ParseDependency(string address)
    {
        if (string.IsNullOrWhiteSpace(address)) throw new ArgumentException("Dependency address is required.", nameof(address));
        var bang = address.IndexOf('!');
        var colon = address.IndexOf(':', bang + 1);
        var slash = address.IndexOf('/', colon + 1);
        if (bang <= 0 || colon <= bang || slash <= colon || slash == address.Length - 1) throw new FormatException($"Invalid workbook dependency address '{address}'.");
        return new CellDependency(address[..bang], address[(bang + 1)..colon], address[(colon + 1)..slash], address[(slash + 1)..]);
    }
}
