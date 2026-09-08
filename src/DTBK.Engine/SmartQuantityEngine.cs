using System.Globalization;

namespace DTBK.Engine;

public sealed record QuantityEvaluation(decimal Value, string NormalizedExpression, IReadOnlyList<string> Tokens);

public sealed class SmartQuantityEngine
{
    public decimal Evaluate(string input) => EvaluateDetailed(input).Value;
    public QuantityEvaluation EvaluateDetailed(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) throw new ArgumentException("Quantity is empty.", nameof(input));
        var tokens = Tokenizer.Tokenize(input); var parser = new Parser(tokens); var ast = parser.ParseExpression(); parser.ExpectEnd(); return new(ast.Evaluate(), string.Join(" ", tokens.Select(t => t.Text)), tokens.Select(t => t.Text).ToArray());
    }
    private enum TokenKind { Number, Plus, Minus, Multiply, Divide, LeftParen, RightParen, End }
    private sealed record Token(TokenKind Kind, string Text, decimal Value = 0m);
    private static class Tokenizer
    {
        public static List<Token> Tokenize(string input)
        {
            var s = input.Trim().Replace('×', '*').Replace('÷', '/').Replace('−', '-'); var list = new List<Token>(); int i = 0;
            while (i < s.Length) { char ch = s[i]; if (char.IsWhiteSpace(ch)) { i++; continue; } if (ch == ',') ch = '.'; if (char.IsDigit(ch) || ch == '.') { int start = i; bool dot = false; while (i < s.Length) { char c = s[i]; if (char.IsDigit(c)) { i++; continue; } if (c == '.' && !dot) { dot = true; i++; continue; } break; } var text = s[start..i]; if (!decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var v)) throw new FormatException($"Invalid number '{text}'."); list.Add(new(TokenKind.Number, text, v)); continue; } var kind = ch switch { '+' => TokenKind.Plus, '-' => TokenKind.Minus, '*' => TokenKind.Multiply, '/' => TokenKind.Divide, '(' => TokenKind.LeftParen, ')' => TokenKind.RightParen, _ => throw new FormatException($"Invalid character '{ch}'.") }; list.Add(new(kind, ch.ToString())); i++; }
            list.Add(new(TokenKind.End, "<END>")); return list;
        }
    }
    private abstract record Node { public abstract decimal Evaluate(); }
    private sealed record NumberNode(decimal Value) : Node { public override decimal Evaluate() => Value; }
    private sealed record UnaryNode(TokenKind Op, Node Operand) : Node { public override decimal Evaluate() => Op == TokenKind.Minus ? -Operand.Evaluate() : Operand.Evaluate(); }
    private sealed record BinaryNode(TokenKind Op, Node Left, Node Right) : Node { public override decimal Evaluate() { var a = Left.Evaluate(); var b = Right.Evaluate(); return Op switch { TokenKind.Plus => a + b, TokenKind.Minus => a - b, TokenKind.Multiply => a * b, TokenKind.Divide => b == 0m ? throw new DivideByZeroException("Division by zero.") : a / b, _ => throw new InvalidOperationException() }; } }
    private sealed class Parser
    {
        readonly List<Token> t; int i; public Parser(List<Token> tokens) => t = tokens;
        public Node ParseExpression() { var n = ParseTerm(); while (Current.Kind is TokenKind.Plus or TokenKind.Minus) { var op = Current.Kind; Next(); n = new BinaryNode(op, n, ParseTerm()); } return n; }
        Node ParseTerm() { var n = ParseUnary(); while (Current.Kind is TokenKind.Multiply or TokenKind.Divide) { var op = Current.Kind; Next(); n = new BinaryNode(op, n, ParseUnary()); } return n; }
        Node ParseUnary() { if (Current.Kind is TokenKind.Plus or TokenKind.Minus) { var op = Current.Kind; Next(); return new UnaryNode(op, ParseUnary()); } return ParsePrimary(); }
        Node ParsePrimary() { if (Current.Kind == TokenKind.Number) { var n = new NumberNode(Current.Value); Next(); return n; } if (Current.Kind == TokenKind.LeftParen) { Next(); var n = ParseExpression(); if (Current.Kind != TokenKind.RightParen) throw new FormatException("Missing ')'."); Next(); return n; } throw new FormatException($"Unexpected '{Current.Text}'."); }
        public void ExpectEnd() { if (Current.Kind != TokenKind.End) throw new FormatException($"Unexpected '{Current.Text}'."); } Token Current => t[i]; void Next() { if (i < t.Count - 1) i++; }
    }
}
