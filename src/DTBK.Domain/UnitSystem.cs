namespace DTBK.Domain;

/// <summary>
/// Canonical unit identity used by calculations. Language labels and source text are presentation/evidence only.
/// </summary>
public sealed record UnitDefinition(
    string Code,
    string Dimension,
    string Symbol,
    string VietnameseLabel,
    string EnglishLabel,
    decimal FactorToBase = 1m);

/// <summary>Normalized unit value. RawSourceUnit is preserved verbatim for provenance.</summary>
public sealed record NormalizedUnit(
    string RawSourceUnit,
    string Code,
    string Dimension,
    string CanonicalSymbol,
    string VietnameseLabel,
    string EnglishLabel,
    decimal FactorToBase,
    decimal CanonicalQuantity,
    string Language)
{
    public string DisplayLabel(string language) =>
        string.Equals(language, "en", StringComparison.OrdinalIgnoreCase) ? EnglishLabel : VietnameseLabel;
}

/// <summary>
/// Deterministic VI/EN unit normalization. It never mutates the source value.
/// </summary>
public static class UnitNormalizer
{
    private static readonly IReadOnlyDictionary<string, UnitDefinition> Definitions =
        new Dictionary<string, UnitDefinition>(StringComparer.OrdinalIgnoreCase)
        {
            ["MM"] = new("MM", "length", "mm", "milimét", "millimetre", 0.001m),
            ["CM"] = new("CM", "length", "cm", "centimét", "centimetre", 0.01m),
            ["M"] = new("M", "length", "m", "mét", "metre"),
            ["KM"] = new("KM", "length", "km", "kilômét", "kilometre", 1000m),
            ["M2"] = new("M2", "area", "m²", "mét vuông", "square metre"),
            ["M3"] = new("M3", "volume", "m³", "mét khối", "cubic metre"),
            ["KG"] = new("KG", "mass", "kg", "kilôgam", "kilogram"),
            ["G"] = new("G", "mass", "g", "gam", "gram", 0.001m),
            ["T"] = new("T", "mass", "t", "tấn", "tonne", 1000m),
            ["H"] = new("H", "time", "h", "giờ", "hour"),
            ["CA"] = new("CA", "labor_shift", "ca", "ca", "shift"),
            ["CONG"] = new("CONG", "labor_day", "công", "công", "labor-day"),
            ["EA"] = new("EA", "count", "cái", "cái", "each"),
            ["L"] = new("L", "volume", "L", "lít", "litre", 0.001m),
            ["PCT"] = new("PCT", "ratio", "%", "%", "%")
        };

    private static readonly IReadOnlyDictionary<string, string> Aliases = BuildAliases();

    public static NormalizedUnit Normalize(string rawUnit, decimal quantity = 1m, string language = "vi")
    {
        if (string.IsNullOrWhiteSpace(rawUnit))
            throw new ArgumentException("Unit is required.", nameof(rawUnit));

        var raw = rawUnit.Trim();
        var key = NormalizeKey(raw);
        if (!Aliases.TryGetValue(key, out var code) || !Definitions.TryGetValue(code, out var definition))
            throw new ArgumentException($"Unsupported unit: '{raw}'.", nameof(rawUnit));

        return new NormalizedUnit(
            raw,
            definition.Code,
            definition.Dimension,
            definition.Symbol,
            definition.VietnameseLabel,
            definition.EnglishLabel,
            definition.FactorToBase,
            quantity * definition.FactorToBase,
            NormalizeLanguage(language));
    }

    public static decimal ConvertToCanonical(string rawUnit, decimal quantity) => Normalize(rawUnit, quantity).CanonicalQuantity;

    public static string Display(string rawUnit, string language)
    {
        var normalized = Normalize(rawUnit, 1m, language);
        return normalized.CanonicalSymbol;
    }

    public static bool AreCompatible(string leftUnit, string rightUnit)
    {
        return string.Equals(Normalize(leftUnit).Dimension, Normalize(rightUnit).Dimension, StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeLanguage(string language) =>
        string.Equals(language, "en", StringComparison.OrdinalIgnoreCase) ? "en" : "vi";

    private static string NormalizeKey(string value)
    {
        var key = value.Trim().ToLowerInvariant()
            .Replace("²", "2", StringComparison.Ordinal)
            .Replace("³", "3", StringComparison.Ordinal)
            .Replace(".", "", StringComparison.Ordinal)
            .Replace("_", "", StringComparison.Ordinal)
            .Replace("-", "", StringComparison.Ordinal)
            .Replace(" ", "", StringComparison.Ordinal);
        return key;
    }

    private static IReadOnlyDictionary<string, string> BuildAliases()
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        Add(map, "MM", "mm", "millimeter", "millimetre", "milimet", "milimét");
        Add(map, "CM", "cm", "centimeter", "centimetre", "centimet", "centimét");
        Add(map, "M", "m", "meter", "metre", "met", "mét");
        Add(map, "KM", "km", "kilometer", "kilometre", "kilomet", "kilômét");
        Add(map, "M2", "m2", "m²", "sqm", "squaremeter", "metvuong", "métvuông");
        Add(map, "M3", "m3", "m³", "cum", "cbm", "cubicmeter", "metkhoi", "métkhối");
        Add(map, "KG", "kg", "kilogram", "kilograms", "kilogam", "kilôgam");
        Add(map, "G", "g", "gram", "grams", "gam");
        Add(map, "T", "t", "ton", "tons", "tonne", "tonnes", "tan", "tấn");
        Add(map, "H", "h", "hr", "hrs", "hour", "hours", "gio", "giờ");
        Add(map, "CA", "ca", "shift", "shifts");
        Add(map, "CONG", "cong", "công", "labor-day", "laborday", "labor day");
        Add(map, "EA", "ea", "each", "item", "pcs", "piece", "pieces", "cai", "cái");
        Add(map, "L", "l", "liter", "litre", "liters", "litres", "lit", "lít");
        Add(map, "PCT", "%", "pct", "percent", "percentage");
        return map;
    }

    private static void Add(Dictionary<string, string> map, string code, params string[] aliases)
    {
        foreach (var alias in aliases) map[NormalizeKey(alias)] = code;
    }
}
