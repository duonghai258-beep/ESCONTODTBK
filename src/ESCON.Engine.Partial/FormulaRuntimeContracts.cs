using System;
using System.Collections.Generic;
using DTBK.Domain;

namespace DTBK.Engine;

public interface IFormulaRuntimeProvider { FormulaFunctionResult Evaluate(FormulaFunctionRequest request); }
public interface ISpValueProvider { FormulaValueResult Resolve(FormulaFunctionRequest request); }
public interface ISumGroupProvider { FormulaValueResult Aggregate(FormulaFunctionRequest request); }

public sealed record FormulaFunctionRequest
{
    public string FunctionName { get; }
    public IReadOnlyList<object?> Arguments { get; }
    public CalculationContext Context { get; }
    public string? CurrentRowKey { get; }
    public string? CurrentGroupKey { get; }
    public FormulaFunctionRequest(string functionName, IReadOnlyList<object?>? arguments, CalculationContext context, string? currentRowKey = null, string? currentGroupKey = null)
    {
        if (string.IsNullOrWhiteSpace(functionName)) throw new ArgumentException("Function name is required.", nameof(functionName));
        ArgumentNullException.ThrowIfNull(context);
        FunctionName = functionName; Arguments = arguments ?? Array.Empty<object?>(); Context = context; CurrentRowKey = currentRowKey; CurrentGroupKey = currentGroupKey;
    }
}

public sealed record FormulaDependency
{
    public string Key { get; }
    public string Kind { get; }
    public FormulaDependency(string key, string kind)
    {
        if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Dependency key is required.", nameof(key));
        if (string.IsNullOrWhiteSpace(kind)) throw new ArgumentException("Dependency kind is required.", nameof(kind));
        Key = key; Kind = kind;
    }
}

public sealed record FormulaProvenance
{
    public string Source { get; }
    public string? SourceVersion { get; }
    public string EvidenceStatus { get; }
    public string? DocumentNo { get; }
    public string? RuleId { get; }
    public DateTime? EffectiveFrom { get; }
    public DateTime? EffectiveTo { get; }
    public string? Applicability { get; }
    public string? FormulaMetadata { get; }
    public string? RoundingRule { get; }
    public FormulaProvenance(string source, string? sourceVersion, string evidenceStatus, string? documentNo = null, string? ruleId = null, DateTime? effectiveFrom = null, DateTime? effectiveTo = null, string? applicability = null, string? formulaMetadata = null, string? roundingRule = null)
    {
        if (string.IsNullOrWhiteSpace(source)) throw new ArgumentException("Provenance source is required.", nameof(source));
        if (string.IsNullOrWhiteSpace(evidenceStatus)) throw new ArgumentException("Evidence status is required.", nameof(evidenceStatus));
        Source = source; SourceVersion = sourceVersion; EvidenceStatus = evidenceStatus; DocumentNo = documentNo; RuleId = ruleId; EffectiveFrom = effectiveFrom; EffectiveTo = effectiveTo; Applicability = applicability; FormulaMetadata = formulaMetadata; RoundingRule = roundingRule;
    }
    public bool HasCanonicalFields => !string.IsNullOrWhiteSpace(DocumentNo) && !string.IsNullOrWhiteSpace(RuleId) && !string.IsNullOrWhiteSpace(Applicability) && !string.IsNullOrWhiteSpace(FormulaMetadata) && !string.IsNullOrWhiteSpace(RoundingRule);
    public CalculationProvenance? TryCreateCanonical(DTBK.Domain.VerificationStatus status) => HasCanonicalFields ? new CalculationProvenance(Source, DocumentNo!, RuleId!, EffectiveFrom, EffectiveTo, Applicability!, FormulaMetadata!, RoundingRule!, status) : null;
}

public sealed record FormulaTraceStep
{
    public string FunctionName { get; }
    public string InputDescription { get; }
    public string ResultDescription { get; }
    public string Status { get; }
    public FormulaTraceStep(string functionName, string inputDescription, string resultDescription, string status)
    {
        if (string.IsNullOrWhiteSpace(functionName)) throw new ArgumentException("Function name is required.", nameof(functionName));
        if (string.IsNullOrWhiteSpace(status)) throw new ArgumentException("Trace status is required.", nameof(status));
        FunctionName = functionName; InputDescription = inputDescription ?? string.Empty; ResultDescription = resultDescription ?? string.Empty; Status = status;
    }
}

public sealed record FormulaValueResult
{
    public object? Value { get; }
    public DTBK.Domain.VerificationStatus VerificationStatus { get; }
    public IReadOnlyList<FormulaDependency> Dependencies { get; }
    public FormulaProvenance? Provenance { get; }
    public FormulaTraceStep Trace { get; }
    public string? ErrorCode { get; }
    public string? ErrorMessage { get; }
    public FormulaValueResult(object? value, DTBK.Domain.VerificationStatus verificationStatus, IReadOnlyList<FormulaDependency>? dependencies, FormulaProvenance? provenance, FormulaTraceStep trace, string? errorCode = null, string? errorMessage = null)
    {
        ArgumentNullException.ThrowIfNull(trace);
        if (verificationStatus == DTBK.Domain.VerificationStatus.Verified && provenance is null) throw new ArgumentException("Verified formula results require provenance.", nameof(provenance));
        if (string.IsNullOrWhiteSpace(errorCode) != string.IsNullOrWhiteSpace(errorMessage)) throw new ArgumentException("ErrorCode and ErrorMessage must be supplied together.");
        Value = value; VerificationStatus = verificationStatus; Dependencies = dependencies ?? Array.Empty<FormulaDependency>(); Provenance = provenance; Trace = trace; ErrorCode = errorCode; ErrorMessage = errorMessage;
    }
    public static FormulaValueResult Blocked(FormulaFunctionRequest request, string code, string message) => new(null, DTBK.Domain.VerificationStatus.Blocked, Array.Empty<FormulaDependency>(), null, new FormulaTraceStep(request.FunctionName, Describe(request), message, "Blocked"), code, message);
    public static FormulaValueResult NeedsVerification(FormulaFunctionRequest request, object? value, IReadOnlyList<FormulaDependency> dependencies, FormulaProvenance? provenance, string message) => new(value, DTBK.Domain.VerificationStatus.NeedsVerification, dependencies, provenance, new FormulaTraceStep(request.FunctionName, Describe(request), message, "NeedsVerification"));
    private static string Describe(FormulaFunctionRequest request) => $"args={request.Arguments.Count};row={request.CurrentRowKey ?? "<none>"};group={request.CurrentGroupKey ?? "<none>"}";
}

public sealed record FormulaFunctionResult
{
    public FormulaValueResult ValueResult { get; }
    public string? CanonicalFunctionName { get; }
    public FormulaFunctionResult(FormulaValueResult valueResult, string? canonicalFunctionName = null) { ArgumentNullException.ThrowIfNull(valueResult); ValueResult = valueResult; CanonicalFunctionName = canonicalFunctionName; }
}

public sealed class FormulaRuntime : IFormulaRuntimeProvider
{
    private readonly ISpValueProvider? _spValueProvider;
    private readonly ISumGroupProvider? _sumGroupProvider;
    public FormulaRuntime(ISpValueProvider? spValueProvider = null, ISumGroupProvider? sumGroupProvider = null) { _spValueProvider = spValueProvider; _sumGroupProvider = sumGroupProvider; }
    public FormulaFunctionResult Evaluate(FormulaFunctionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var function = request.FunctionName.Trim();
        if (function.Equals("SPVALUE", StringComparison.OrdinalIgnoreCase))
        {
            var canonicalRequest = Canonicalize(request, "SPVALUE");
            return new FormulaFunctionResult(_spValueProvider is null ? FormulaValueResult.Blocked(canonicalRequest, "FORMULA_PROVIDER_UNAVAILABLE", "SPVALUE provider is not available.") : _spValueProvider.Resolve(canonicalRequest), "SPVALUE");
        }
        if (function.Equals("SUMGROUP", StringComparison.OrdinalIgnoreCase))
        {
            var canonicalRequest = Canonicalize(request, "SUMGROUP");
            return new FormulaFunctionResult(_sumGroupProvider is null ? FormulaValueResult.Blocked(canonicalRequest, "FORMULA_PROVIDER_UNAVAILABLE", "SUMGROUP provider is not available.") : _sumGroupProvider.Aggregate(canonicalRequest), "SUMGROUP");
        }
        return new FormulaFunctionResult(FormulaValueResult.Blocked(request, "FORMULA_FUNCTION_UNSUPPORTED", $"Formula function '{function}' is not evidence-backed by this runtime contract."), function);
    }
    private static FormulaFunctionRequest Canonicalize(FormulaFunctionRequest request, string canonicalName) => new(canonicalName, request.Arguments, request.Context, request.CurrentRowKey, request.CurrentGroupKey);
}
