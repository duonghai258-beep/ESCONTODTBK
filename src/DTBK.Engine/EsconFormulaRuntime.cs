using System.Globalization;
using System.Text.RegularExpressions;

namespace DTBK.Engine;

/// <summary>Evidence-bounded runtime for observed ESCON formula constructs.</summary>
public sealed class EsconFormulaRuntime
{
    // ESCON binary evidence uses SPVALUE\(([^\)]*)\): observed formulas are unquoted
    // identifiers (e.g. SPVALUE(VAT)). Keep quoted forms supported for compatibility.
    private static readonly Regex SpValueToken = new(
        @"SPVALUE\s*\(\s*(?<key>[^)]*?)\s*\)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private readonly IEsconValueProvider _provider;
    private readonly IReadOnlyList<IReadOnlyDictionary<string, decimal?>> _groupRows;
    private readonly SpecialValueStore _specialValues;

    public EsconFormulaRuntime(
        IEsconValueProvider provider,
        IReadOnlyList<IReadOnlyDictionary<string, decimal?>>? groupRows = null,
        SpecialValueStore? specialValues = null)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        _groupRows = groupRows ?? Array.Empty<IReadOnlyDictionary<string, decimal?>>();
        _specialValues = specialValues ?? new SpecialValueStore();
    }

    public SpecialValueStore SpecialValues => _specialValues;

    public decimal? Evaluate(string expression, IReadOnlyDictionary<string, decimal?> row)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(expression);
        ArgumentNullException.ThrowIfNull(row);
        var preprocessed = PreprocessSpecialValues(expression);
        return new Parser(this, preprocessed, row).Parse();
    }

    private string PreprocessSpecialValues(string formula)
    {
        return SpValueToken.Replace(formula, match =>
        {
            var key = match.Groups["key"].Value.Trim().Trim('\"', '\'');
            if (!_specialValues.Contains(key))
                return match.Value;

            var value = _specialValues[key];
            return value.ToString();
        });
    }

    private decimal? Field(string name, IReadOnlyDictionary<string, decimal?> row)
        => row.TryGetValue(name, out var value) ? value : _provider.ResolveField(name);

    private decimal? SumGroup(string field)
    {
        decimal total = 0m;
        foreach (var row in _groupRows)
            if (row.TryGetValue(field, out var value) && value.HasValue) total += value.Value;
        return total;
    }

    private sealed class Parser
    {
        private readonly EsconFormulaRuntime _rt;
        private readonly string _s;
        private readonly IReadOnlyDictionary<string, decimal?> _row;
        private int _p;
        public Parser(EsconFormulaRuntime rt, string s, IReadOnlyDictionary<string, decimal?> row) { _rt = rt; _s = s; _row = row; }
        public decimal? Parse() { var v = Comparison(); Skip(); if (_p != _s.Length) throw Error("unexpected trailing content"); return v; }
        private decimal? Comparison()
        {
            var l = Additive(); Skip();
            if (Take("<>")) return Bool(l != Additive());
            if (Take(">=")) return Compare(l, Additive(), (a,b)=>a>=b);
            if (Take("<=")) return Compare(l, Additive(), (a,b)=>a<=b);
            if (Take(">")) return Compare(l, Additive(), (a,b)=>a>b);
            if (Take("<")) return Compare(l, Additive(), (a,b)=>a<b);
            if (Take("=")) return Bool(l == Additive());
            return l;
        }
        private static decimal? Compare(decimal? a, decimal? b, Func<decimal,decimal,bool> op) => a.HasValue && b.HasValue ? Bool(op(a.Value,b.Value)) : null;
        private decimal? Additive() { var v=Multiplicative(); while(true){Skip(); if(Take("+")) v=Bin(v,Multiplicative(),(a,b)=>a+b); else if(Take("-")) v=Bin(v,Multiplicative(),(a,b)=>a-b); else return v;} }
        private decimal? Multiplicative() { var v=Unary(); while(true){Skip(); if(Take("*")) v=Bin(v,Unary(),(a,b)=>a*b); else if(Take("/")){var r=Unary(); if(r==0) throw new DivideByZeroException(); v=Bin(v,r,(a,b)=>a/b); } else return v;} }
        private decimal? Unary(){Skip(); if(Take("+")) return Unary(); if(Take("-")){var v=Unary(); return v.HasValue ? -v : null;} return Primary();}
        private decimal? Primary()
        {
            Skip();
            if(Take("(")){var v=Comparison(); Expect(")"); return v;}
            if(_p<_s.Length && char.IsDigit(_s[_p])) return Number();
            if(Take("[")){var n=Until(']'); Expect("]"); return _rt.Field(n,_row);}
            var id=Identifier(); if(id.Length==0) throw Error("expected value"); Skip();
            if(!Take("(")) return _rt.Field(id,_row);
            var args=Arguments();
            return id.ToUpperInvariant() switch
            {
                "SUMGROUP" when args.Count==1 => _rt.SumGroup(UnwrapField(args[0])),
                "IF" when args.Count==3 => new Parser(_rt, IsTrue(new Parser(_rt,args[0],_row).Parse()) ? args[1] : args[2], _row).Parse(),
                _ => throw Error($"unsupported ESCON function '{id}' or invalid argument count")
            };
        }
        private List<string> Arguments(){var a=new List<string>(); var start=_p; var depth=0; var quote=false; while(_p<_s.Length){var c=_s[_p++]; if(c=='\''||c=='\"') quote=!quote; else if(!quote&&c=='(') depth++; else if(!quote&&c==')'){if(depth==0){Add(a,start,_p-1); return a;} depth--;} else if(!quote&&(c==','||c==';')&&depth==0){Add(a,start,_p-1); start=_p;}} throw Error("unclosed argument list");}
        private void Add(List<string> a,int start,int end){var x=_s[start..end].Trim(); if(x.Length==0) throw Error("empty argument"); a.Add(x);}
        private decimal? Number(){var st=_p; while(_p<_s.Length&&(char.IsDigit(_s[_p])||_s[_p]=='.'))_p++; if(!decimal.TryParse(_s[st.._p],NumberStyles.Number,CultureInfo.InvariantCulture,out var v))throw Error("invalid number"); return v;}
        private string Identifier(){var st=_p; while(_p<_s.Length&&(char.IsLetterOrDigit(_s[_p])||_s[_p]=='_'))_p++; return _s[st.._p];}
        private string Until(char end){var st=_p; while(_p<_s.Length&&_s[_p]!=end)_p++; return _s[st.._p].Trim();}
        private void Expect(string x){Skip(); if(!Take(x))throw Error($"expected '{x}'");}
        private bool Take(string x){if(!_s.AsSpan(_p).StartsWith(x,StringComparison.Ordinal))return false;_p+=x.Length;return true;}
        private void Skip(){while(_p<_s.Length&&char.IsWhiteSpace(_s[_p]))_p++;}
        private static decimal? Bin(decimal? a,decimal? b,Func<decimal,decimal,decimal> f)=>a.HasValue&&b.HasValue?f(a.Value,b.Value):null;
        private static decimal? Bool(bool b)=>b?1m:0m;
        private static bool IsTrue(decimal? v)=>v.HasValue&&v.Value!=0m;
        private static string UnwrapField(string s)=>s.Trim().TrimStart('[').TrimEnd(']').Trim();
        private InvalidOperationException Error(string m)=>new($"ESCON formula parse error at {_p}: {m}");
    }
}

public interface IEsconValueProvider
{
    decimal? ResolveSpecialValue(string key);
    decimal? ResolveField(string name);
}
