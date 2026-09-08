using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace DTBK.Engine;

public sealed class EvidenceFormulaEngine
{
    private static readonly Regex FieldPattern = new(@"\[(?<name>[^\]]+)\]", RegexOptions.Compiled);
    public FormulaValueResult Evaluate(string expression, FormulaEvaluationContext context)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(expression); ArgumentNullException.ThrowIfNull(context);
        var dependencies = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var parser = new Parser(expression, context, dependencies); var value = parser.ParseExpression(); parser.ExpectEnd();
        return new FormulaValueResult(value, DTBK.Domain.VerificationStatus.NeedsVerification,
            dependencies.Select(x => new FormulaDependency(x.Key, x.Value)).ToArray(), null,
            new FormulaTraceStep("FORMULA", expression, value.ToString(CultureInfo.InvariantCulture), "NeedsVerification"));
    }
    private sealed class Parser
    {
        private readonly string _text; private readonly FormulaEvaluationContext _context; private readonly IDictionary<string,string> _dependencies; private int _position;
        public Parser(string text, FormulaEvaluationContext context, IDictionary<string,string> dependencies){_text=Normalize(text);_context=context;_dependencies=dependencies;}
        public decimal ParseExpression()=>ParseAdditive();
        public void ExpectEnd(){SkipWhite();if(_position!=_text.Length)throw Error($"Unexpected token at position {_position}: '{_text[_position..]}'.");}
        private decimal ParseAdditive(){var value=ParseMultiplicative();while(true){SkipWhite();if(Match('+'))value+=ParseMultiplicative();else if(Match('-'))value-=ParseMultiplicative();else return value;}}
        private decimal ParseMultiplicative(){var value=ParseUnary();while(true){SkipWhite();if(Match('*'))value*=ParseUnary();else if(Match('/')){var divisor=ParseUnary();if(divisor==0m)throw Error("Division by zero.");value/=divisor;}else return value;}}
        private decimal ParseUnary(){SkipWhite();if(Match('+'))return ParseUnary();if(Match('-'))return-ParseUnary();return ParsePrimary();}
        private decimal ParsePrimary(){SkipWhite();if(Match('(')){var value=ParseExpression();Expect(')');return value;}if(Peek('['))return ResolveField();if(char.IsDigit(Current)||Current=='.')return ParseNumber();if(char.IsLetter(Current)||Current=='_')return ParseFunctionOrIdentifier();throw Error($"Expected value at position {_position}.");}
        private decimal ResolveField(){var start=++_position;while(_position<_text.Length&&_text[_position]!=']')_position++;if(_position>=_text.Length)throw Error("Unterminated field reference.");var name=_text[start.._position].Trim();_position++;if(!_context.TryGetValue(name,out var value))throw Error($"Missing field '{name}'.");_dependencies[$"FIELD:{name}"]=name;return value;}
        private decimal ParseFunctionOrIdentifier(){var name=ParseIdentifier();SkipWhite();if(!Match('('))throw Error($"Unsupported bare identifier '{name}'.");return name.ToUpperInvariant() switch{"IF"=>ParseIf(),"OR"=>ParseOr(),"SPVALUE"=>ParseSpValue(),"SUMGROUP"=>ParseSumGroup(),_=>throw Error($"Unsupported function '{name}'.")};}
        private decimal ParseIf(){var condition=ParseCondition();ExpectArgumentSeparator();var whenTrue=ParseExpression();ExpectArgumentSeparator();var whenFalse=ParseExpression();Expect(')');return condition?whenTrue:whenFalse;}
        private bool ParseCondition(){var left=ParseExpression();SkipWhite();if(Match('='))return left==ParseExpression();if(Match('<'))return Match('=')?left<=ParseExpression():left<ParseExpression();if(Match('>'))return Match('=')?left>=ParseExpression():left>ParseExpression();return left!=0m;}
        private decimal ParseOr(){var result=ParseBooleanArgument();while(true){SkipWhite();if(!MatchArgumentSeparator())break;result|=ParseBooleanArgument();}Expect(')');return result?1m:0m;}
        private bool ParseBooleanArgument(){var left=ParseExpression();SkipWhite();if(Match('='))return left==ParseExpression();if(Match('<'))return Match('=')?left<=ParseExpression():left<ParseExpression();if(Match('>'))return Match('=')?left>=ParseExpression():left>ParseExpression();return left!=0m;}
        private decimal ParseSpValue(){var key=ParseNameArgument();Expect(')');if(!_context.TryResolveSpecialValue(key,out var value))throw Error($"SPVALUE '{key}' was not resolved.");_dependencies[$"SPVALUE:{key}"]=key;return value;}
        private decimal ParseSumGroup(){var field=ParseNameArgument();Expect(')');var value=_context.SumGroup(field);_dependencies[$"SUMGROUP:{field}"]=$"{field}@{_context.GroupKey}";return value;}
        private string ParseNameArgument(){SkipWhite();if(Peek('[')){var start=++_position;while(_position<_text.Length&&_text[_position]!=']')_position++;if(_position>=_text.Length)throw Error("Unterminated name argument.");var name=_text[start.._position].Trim();_position++;return name;}return ParseIdentifier();}
        private string ParseIdentifier(){SkipWhite();var start=_position;while(_position<_text.Length&&(char.IsLetterOrDigit(_text[_position])||_text[_position]=='_'))_position++;if(start==_position)throw Error($"Expected identifier at position {_position}.");return _text[start.._position];}
        private decimal ParseNumber(){var start=_position;while(_position<_text.Length&&(char.IsDigit(_text[_position])||_text[_position]=='.'))_position++;var token=_text[start.._position];if(!decimal.TryParse(token,NumberStyles.Number,CultureInfo.InvariantCulture,out var value))throw Error($"Invalid number '{token}'.");return value;}
        private void ExpectArgumentSeparator(){SkipWhite();if(Match(';')||Match(','))return;throw Error("Expected function argument separator ';' or ','.");}
        private bool MatchArgumentSeparator(){SkipWhite();return Match(';')||Match(',');}
        private void Expect(char character){SkipWhite();if(!Match(character))throw Error($"Expected '{character}'.");}
        private bool Match(char character){if(_position<_text.Length&&_text[_position]==character){_position++;return true;}return false;}
        private bool Peek(char character)=>_position<_text.Length&&_text[_position]==character;private char Current=>_position<_text.Length?_text[_position]:'\0';private void SkipWhite(){while(_position<_text.Length&&char.IsWhiteSpace(_text[_position]))_position++;}private FormatException Error(string message)=>new(message);
        private static string Normalize(string expression){var builder=new StringBuilder(expression.Length);foreach(var ch in expression)builder.Append(ch=='×'?'*':ch=='÷'?'/':ch);return builder.ToString();}
    }
}

public sealed class FormulaEvaluationContext
{
    private readonly IReadOnlyDictionary<string,decimal> _values;private readonly IReadOnlyDictionary<string,decimal> _specialValues;private readonly IReadOnlyList<FormulaRow> _rows;
    public FormulaEvaluationContext(IReadOnlyDictionary<string,decimal> values,IReadOnlyDictionary<string,decimal>? specialValues=null,IReadOnlyList<FormulaRow>? rows=null,string groupKey=""){_values=new Dictionary<string,decimal>(values,StringComparer.OrdinalIgnoreCase);_specialValues=new Dictionary<string,decimal>(specialValues??new Dictionary<string,decimal>(),StringComparer.OrdinalIgnoreCase);_rows=rows??Array.Empty<FormulaRow>();GroupKey=groupKey??string.Empty;}
    public string GroupKey{get;} public bool TryGetValue(string name,out decimal value)=>_values.TryGetValue(name,out value); public bool TryResolveSpecialValue(string name,out decimal value)=>_specialValues.TryGetValue(name,out value);
    public decimal SumGroup(string field){var selected=string.IsNullOrWhiteSpace(GroupKey)?_rows:_rows.Where(x=>string.Equals(x.GroupKey,GroupKey,StringComparison.OrdinalIgnoreCase)).ToArray();return selected.Sum(x=>x.Get(field));}
}
public sealed record FormulaRow(string GroupKey,IReadOnlyDictionary<string,decimal> Values){public decimal Get(string field)=>Values.TryGetValue(field,out var value)?value:throw new KeyNotFoundException($"SUMGROUP field '{field}' is missing from row group '{GroupKey}'.");}
