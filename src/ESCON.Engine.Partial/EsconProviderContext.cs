using DTBK.Domain;

namespace DTBK.Engine;

/// <summary>
/// Explicit ESCON-to-DTBK provider bridge. No guessed database schema is introduced:
/// callers supply the authoritative provider delegates and calculation context.
/// </summary>
public sealed class EsconProviderContext : IEsconValueProvider
{
    private readonly IReadOnlyDictionary<string, decimal?> _specialValues;
    private readonly IReadOnlyDictionary<string, decimal?> _fields;
    private readonly Func<string, decimal?>? _specialResolver;
    private readonly Func<string, decimal?>? _fieldResolver;

    public EsconProviderContext(
        IReadOnlyDictionary<string, decimal?>? specialValues = null,
        IReadOnlyDictionary<string, decimal?>? fields = null,
        Func<string, decimal?>? specialResolver = null,
        Func<string, decimal?>? fieldResolver = null)
    {
        _specialValues = specialValues ?? new Dictionary<string, decimal?>();
        _fields = fields ?? new Dictionary<string, decimal?>();
        _specialResolver = specialResolver;
        _fieldResolver = fieldResolver;
    }

    public decimal? ResolveSpecialValue(string key)
    {
        if (_specialValues.TryGetValue(key, out var value)) return value;
        return _specialResolver?.Invoke(key);
    }

    public decimal? ResolveField(string name)
    {
        if (_fields.TryGetValue(name, out var value)) return value;
        return _fieldResolver?.Invoke(name);
    }
}

/// <summary>Factory for wiring the ESCON runtime to the existing DTBK provider contracts.</summary>
public static class EsconProviderContextFactory
{
    public static EsconProviderContext FromProviders(
        INormProvider norms,
        IResourcePriceProvider? resourcePrices = null,
        ILaborPriceProvider? laborPrices = null,
        ICoefficientProvider? coefficients = null,
        int? provinceId = null,
        DateTime? effectiveDate = null,
        WorkType? workType = null,
        LocationType? location = null)
    {
        ArgumentNullException.ThrowIfNull(norms);
        return new EsconProviderContext(
            specialResolver: key => ResolveSpecial(key, norms, resourcePrices, laborPrices, coefficients, provinceId, effectiveDate, workType, location));
    }

    private static decimal? ResolveSpecial(
        string key,
        INormProvider norms,
        IResourcePriceProvider? resourcePrices,
        ILaborPriceProvider? laborPrices,
        ICoefficientProvider? coefficients,
        int? provinceId,
        DateTime? effectiveDate,
        WorkType? workType,
        LocationType? location)
    {
        // Provider bridge is deliberately explicit. Unknown keys remain unresolved rather than guessed.
        var parts = key.Split(':', StringSplitOptions.TrimEntries);
        if (parts.Length >= 3 && parts[0].Equals("RESOURCE", StringComparison.OrdinalIgnoreCase)
            && resourcePrices is not null && provinceId.HasValue && effectiveDate.HasValue
            && Enum.TryParse<ResourceType>(parts[1], true, out var type))
            return resourcePrices.ResolveUnitPrice(type, parts[2], provinceId.Value, effectiveDate.Value);

        if (parts.Length >= 2 && parts[0].Equals("LABOR", StringComparison.OrdinalIgnoreCase)
            && laborPrices is not null && provinceId.HasValue && effectiveDate.HasValue)
            return laborPrices.ResolvePrice(parts[1], provinceId.Value, effectiveDate.Value).UnitPrice;

        if (parts.Length >= 2 && parts[0].Equals("COEFFICIENT", StringComparison.OrdinalIgnoreCase)
            && coefficients is not null && effectiveDate.HasValue && workType.HasValue && location.HasValue)
        {
            var set = coefficients.Resolve(workType.Value, location.Value, effectiveDate.Value, new[] { parts[1] });
            return set.Rules.FirstOrDefault(x => x.Code.Equals(parts[1], StringComparison.OrdinalIgnoreCase))?.Factor;
        }

        if (parts.Length >= 2 && parts[0].Equals("NORM", StringComparison.OrdinalIgnoreCase))
            return norms.GetNorm(parts[1]) is null ? null : 1m;

        return null;
    }
}
