using DTBK.Domain;

namespace DTBK.Engine;

/// <summary>Canonical resource families used by estimation calculation.</summary>
public enum EstimationResourceFamily { Material, Labor, Machine, Fuel, Transport }

public sealed record ResourceResolutionRequest(
    EstimationResourceFamily Family,
    string Code,
    int ProvinceId,
    DateTime EffectiveDate,
    decimal Quantity,
    string Unit,
    string Source = "");

public sealed record ResourceResolution(
    EstimationResourceFamily Family,
    string Code,
    decimal Quantity,
    string Unit,
    decimal? UnitPrice,
    decimal? Amount,
    string PriceVersion,
    string Provenance,
    bool IsResolved,
    string? Diagnostic = null);

public interface IEstimationResourceResolver
{
    ResourceResolution Resolve(ResourceResolutionRequest request);
}

/// <summary>
/// Runtime boundary that forces every resource family through one resolver contract.
/// Existing providers remain the source of truth; this type only normalises their output.
/// </summary>
public sealed class ResourceDomainRuntime
{
    private readonly IEstimationResourceResolver _resolver;

    public ResourceDomainRuntime(IEstimationResourceResolver resolver)
        => _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));

    public ResourceResolution Resolve(ResourceResolutionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Code))
            throw new ArgumentException("Resource code is required.", nameof(request));
        if (request.Quantity < 0) throw new ArgumentOutOfRangeException(nameof(request), "Quantity cannot be negative.");
        return _resolver.Resolve(request);
    }
}

/// <summary>Adapter for an already calculated NormCostBridge result.</summary>
public sealed class ResourceConsumptionResolver : IEstimationResourceResolver
{
    private readonly IReadOnlyDictionary<string, ResourceConsumption> _resources;

    public ResourceConsumptionResolver(IEnumerable<ResourceConsumption> resources)
    {
        _resources = resources
            .Where(x => !string.IsNullOrWhiteSpace(x.ResourceCode))
            .GroupBy(x => x.ResourceCode, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
    }

    public ResourceResolution Resolve(ResourceResolutionRequest request)
    {
        if (!_resources.TryGetValue(request.Code, out var resource))
            return new ResourceResolution(request.Family, request.Code, request.Quantity, request.Unit,
                null, null, "UNRESOLVED", "CalculationState.Resources", false,
                "Resource is not present in the current calculation state.");

        var family = resource.Type switch
        {
            ResourceType.Material => EstimationResourceFamily.Material,
            ResourceType.Labor => EstimationResourceFamily.Labor,
            ResourceType.Machine => EstimationResourceFamily.Machine,
            _ => request.Family
        };
        return new ResourceResolution(family, request.Code, request.Quantity, request.Unit,
            resource.UnitPrice, resource.Amount, "CALCULATION_STATE", "CalculationState.Resources", true);
    }
}
