namespace DTBK.Engine;

public sealed record FormulaDependency(string Key, string Kind = "FORMULA");

public sealed record FormulaValueResult(
    object? Value,
    IReadOnlyList<FormulaDependency> Dependencies,
    string Status = "NeedsVerification",
    string? ErrorCode = null,
    string? ErrorMessage = null);
