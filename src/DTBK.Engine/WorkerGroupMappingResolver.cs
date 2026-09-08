using DTBK.Domain;

namespace DTBK.Engine;

/// <summary>
/// Resolver mapping minh bạch. Không suy đoán từ tên công tác; chỉ trả về mapping
/// đã có bằng chứng/nguồn trong data package và còn hiệu lực.
/// </summary>
public sealed class WorkerGroupMappingResolver
{
    private readonly IReadOnlyList<WorkerGroupMapping> _mappings;
    public WorkerGroupMappingResolver(IEnumerable<WorkerGroupMapping> mappings) => _mappings = mappings.ToList();

    public WorkerGroupMapping Resolve(long workTaskId, DateTime effectiveDate)
    {
        var matches = _mappings.Where(x => x.WorkTaskId == workTaskId
            && (x.EffectiveFrom is null || x.EffectiveFrom.Value.Date <= effectiveDate.Date)
            && (x.EffectiveTo is null || x.EffectiveTo.Value.Date >= effectiveDate.Date)).ToList();

        if (matches.Count == 0)
            throw new InvalidOperationException($"Chưa có mapping nhóm nhân công có bằng chứng cho WorkTaskId={workTaskId} tại {effectiveDate:yyyy-MM-dd}.");
        if (matches.Count > 1)
            throw new InvalidOperationException($"Có nhiều mapping nhóm nhân công hợp lệ cho WorkTaskId={workTaskId}; cần xác định ưu tiên bằng data package.");

        var match = matches[0];
        if (string.IsNullOrWhiteSpace(match.EvidenceReference) || string.IsNullOrWhiteSpace(match.SourceHash))
            throw new InvalidOperationException($"Mapping WorkTaskId={workTaskId} thiếu bằng chứng/hash nguồn.");
        return match;
    }
}
