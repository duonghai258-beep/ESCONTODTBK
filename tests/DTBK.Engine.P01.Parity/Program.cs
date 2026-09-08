using DTBK.Engine;

static void AssertEqual(decimal expected, decimal? actual, string caseName)
{
    if (!actual.HasValue || actual.Value != expected)
        throw new InvalidOperationException($"FAIL {caseName}: expected {expected}, actual {actual?.ToString() ?? "<null>"}");
}

static string EsconObservedPreprocess(string formula, IReadOnlyDictionary<string, object> values)
{
    // Directly mirrors the observed ESCON Cell.cs boundary:
    // detect SPVALUE(...), extract the text inside the parentheses,
    // Contains(key), get_Item(key), Object.ToString(), Replace(token, value).
    var matches = System.Text.RegularExpressions.Regex.Matches(formula, @"SPVALUE\(([^\)]*)\)");
    foreach (System.Text.RegularExpressions.Match match in matches)
    {
        var token = match.Value;
        var key = match.Groups[1].Value.Trim().Trim('\"', '\'');
        if (values.ContainsKey(key))
            formula = formula.Replace(token, values[key].ToString()!);
    }
    return formula;
}

var values = new Dictionary<string, object>(StringComparer.Ordinal)
{
    ["HSPT_OTO"] = 1.10m,
    ["HSPT_XEBEN"] = 1.20m,
    ["HSPT_PTKHAC"] = 1.30m,
    ["HSPT_XE_3T"] = 1.40m,
    ["HSPT_XE3CAU"] = 1.50m,
    ["HSPT_XECOHUTXA"] = 1.60m,
    ["HSBH1"] = 1.70m,
    ["HSBH2"] = 1.80m,
    ["HSBH3"] = 1.90m,
    ["HSBH4"] = 2.00m,
    ["HSLD_1"] = 2.10m,
    ["HSLD_2"] = 2.20m,
    ["HSLD_3"] = 2.30m,
    ["HSLD_4"] = 2.40m,
    ["HSLD_5"] = 2.50m,
    ["HSLD_6"] = 2.60m,
    ["VAT"] = 0.10m,
    ["HSQD_LS1"] = 2.70m,
    ["HSQD_LS2"] = 2.80m,
    ["KhoiLuong"] = 100m,
    ["TongCuoc"] = 12345.67m
};

var provider = new EsconProviderContext();
var store = new SpecialValueStore();
foreach (var pair in values)
    store[pair.Key] = pair.Value;

var runtime = new EsconFormulaRuntime(provider, specialValues: store);
var row = new Dictionary<string, decimal?>();

// All 21 registry keys from the locked P01 evidence inventory.
foreach (var pair in values)
{
    var expression = $"SPVALUE({pair.Key})";
    var observed = EsconObservedPreprocess(expression, values);
    AssertEqual(Convert.ToDecimal(pair.Value), runtime.Evaluate(expression, row), $"key:{pair.Key}");
    if (observed != pair.Value.ToString())
        throw new InvalidOperationException($"FAIL observed-oracle:{pair.Key}");
}

// Exact observed formula family from Sheets.xml for VAT.
const string vatFormula = "100/(1+SPVALUE(VAT))*(1+0.05)";
var observedVatFormula = EsconObservedPreprocess(vatFormula, values);
var expectedVat = 100m / (1m + 0.10m) * (1m + 0.05m);
AssertEqual(expectedVat, runtime.Evaluate(vatFormula, row), "formula:VAT");
if (observedVatFormula != "100/(1+0.10)*(1+0.05)")
    throw new InvalidOperationException("FAIL observed-oracle:VAT formula");

// Multiple SPVALUE tokens in one expression.
const string multiFormula = "SPVALUE(HSPT_OTO)+SPVALUE(HSBH1)*2";
var observedMulti = EsconObservedPreprocess(multiFormula, values);
AssertEqual(1.10m + 1.70m * 2m, runtime.Evaluate(multiFormula, row), "formula:multiple");
if (observedMulti != "1.10+1.70*2")
    throw new InvalidOperationException("FAIL observed-oracle:multiple");

// Quoted compatibility form: accepted by DTBK while preserving the same key lookup boundary.
AssertEqual(0.10m, runtime.Evaluate("SPVALUE(\"VAT\")", row), "quoted:VAT");
AssertEqual(0.10m, runtime.Evaluate("SPVALUE('VAT')", row), "single-quoted:VAT");

// Missing key must not fall back to the provider/database layer.
var fallbackProbe = new ProbeProvider();
var missingRuntime = new EsconFormulaRuntime(fallbackProbe);
try
{
    _ = missingRuntime.Evaluate("SPVALUE(MISSING)", row);
    throw new InvalidOperationException("FAIL missing-key: unexpected successful evaluation");
}
catch (InvalidOperationException ex) when (ex.Message.Contains("unsupported ESCON function", StringComparison.Ordinal))
{
    if (fallbackProbe.SpecialResolveCalls != 0)
        throw new InvalidOperationException("FAIL missing-key: provider fallback was invoked");
}

Console.WriteLine("P01 ESCON↔DTBK PARITY = PASS");
Console.WriteLine("Cases = 21 registry keys + VAT formula + multi-token + quoted compatibility + missing-key no-fallback");

sealed class ProbeProvider : IEsconValueProvider
{
    public int SpecialResolveCalls { get; private set; }
    public decimal? ResolveSpecialValue(string key) { SpecialResolveCalls++; return 999m; }
    public decimal? ResolveField(string name) => null;
}
