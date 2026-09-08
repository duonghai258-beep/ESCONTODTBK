using DTBK.Domain;

namespace DTBK.Engine;

public sealed class CalculationTraceEngine
{
    public CalculationTrace Create(string calculationCode, string inputHash, string outputHash, string engineVersion, IEnumerable<CalculationTraceStep> steps)
    {
        if (string.IsNullOrWhiteSpace(calculationCode)) throw new ArgumentException("Calculation code is required.", nameof(calculationCode));
        var list = steps?.ToArray() ?? throw new ArgumentNullException(nameof(steps));
        if (list.Length == 0) throw new InvalidOperationException("Calculation trace requires at least one step.");
        if (list.Any(x => string.IsNullOrWhiteSpace(x.Stage) || string.IsNullOrWhiteSpace(x.Key)))
            throw new InvalidOperationException("Calculation trace steps require stage and key.");
        return new(calculationCode, inputHash ?? "", outputHash ?? "", engineVersion ?? "", list);
    }
}
