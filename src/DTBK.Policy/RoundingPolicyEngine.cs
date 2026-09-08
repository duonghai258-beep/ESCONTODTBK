using DTBK.Domain;
namespace DTBK.Policy;
public static class RoundingPolicyEngine
{
    public static decimal Round(decimal value, int scale, RoundingMode mode) => mode switch
    {
        RoundingMode.AwayFromZero => Math.Round(value, scale, MidpointRounding.AwayFromZero),
        RoundingMode.ToEven => Math.Round(value, scale, MidpointRounding.ToEven),
        RoundingMode.Floor => Math.Floor(value * Pow10(scale)) / Pow10(scale),
        RoundingMode.Ceiling => Math.Ceiling(value * Pow10(scale)) / Pow10(scale),
        _ => throw new ArgumentOutOfRangeException(nameof(mode))
    };
    public static decimal RoundCurrency(decimal value, RoundingPolicy p) => Round(value, p.CurrencyScale, p.Mode);
    public static decimal RoundRate(decimal value, RoundingPolicy p) => Round(value, p.RateScale, p.Mode);
    public static decimal RoundQuantity(decimal value, RoundingPolicy p) => Round(value, p.QuantityScale, p.Mode);
    static decimal Pow10(int n) => decimal.Parse("1" + new string('0', Math.Max(0, n)), System.Globalization.CultureInfo.InvariantCulture);
}
