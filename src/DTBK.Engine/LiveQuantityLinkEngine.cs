using DTBK.Domain;

namespace DTBK.Engine;

/// <summary>Bridges QTO quantities into the existing cost engine without duplicating pricing logic.</summary>
public sealed class LiveQuantityLinkEngine
{
    public IReadOnlyList<CostLine> BuildCostLines(
        IEnumerable<Measurement> measurements,
        IReadOnlyDictionary<string, long> projectItemByMeasurementId,
        IReadOnlyDictionary<long, ProjectItem>? existingItems = null)
    {
        ArgumentNullException.ThrowIfNull(measurements);
        ArgumentNullException.ThrowIfNull(projectItemByMeasurementId);

        var links = measurements.Select(m =>
        {
            if (!projectItemByMeasurementId.TryGetValue(m.MeasurementId, out var projectItemId))
                throw new InvalidOperationException($"No ProjectItem mapping for measurement '{m.MeasurementId}'.");
            return new QuantityTakeoffEngine().LinkToEstimate(m, projectItemId);
        }).ToList();

        return links.GroupBy(x => x.ProjectItemId).Select(group =>
        {
            var first = group.First();
            if (group.Any(x => !StringComparer.Ordinal.Equals(x.WorkCode, first.WorkCode) ||
                               !StringComparer.Ordinal.Equals(x.NormCode, first.NormCode) ||
                               !StringComparer.Ordinal.Equals(x.Unit, first.Unit)))
            {
                throw new InvalidOperationException(
                    $"ProjectItem {first.ProjectItemId} được liên kết với các measurement có WorkCode/NormCode/Unit không đồng nhất.");
            }

            var quantity = group.Sum(x => x.Quantity);
            var item = existingItems is not null && existingItems.TryGetValue(first.ProjectItemId, out var existing) ? existing : null;
            return new CostLine(
                first.ProjectItemId,
                first.WorkCode,
                item?.Description ?? first.WorkCode,
                first.Unit,
                quantity,
                item?.MaterialAmount ?? 0m,
                item?.LaborAmount ?? 0m,
                item?.MachineAmount ?? 0m,
                first.NormCode);
        }).OrderBy(x => x.ProjectItemID).ToList();
    }
}
