using DTBK.Domain;

namespace DTBK.Engine;

public sealed record EsconIntegrationSnapshot(
    IntegrationVerificationStatus Status,
    string CanonicalEngineType,
    string? EvidenceRoot,
    bool EvidenceDirectoryPresent,
    string Note);

public sealed class EsconIntegrationAdapter
{
    public EsconIntegrationSnapshot Inspect(string? repositoryRoot = null)
    {
        var root = repositoryRoot ?? Directory.GetCurrentDirectory();
        var evidence = Path.Combine(root, "ESCON");
        var engineType = typeof(EsconCalculationAccuracyEngine).FullName
            ?? nameof(EsconCalculationAccuracyEngine);
        var present = Directory.Exists(evidence);

        return new EsconIntegrationSnapshot(
            IntegrationVerificationStatus.PartiallyVerified,
            engineType,
            evidence,
            present,
            present
                ? "ESCON integration is hosted by DTBK.Engine. Formula, SPVALUE and SUMGROUP execution use the existing canonical engine/evaluator; provider-to-database closure remains explicitly unverified until runtime evidence proves it."
                : "ESCON evidence directory was not found at the inspected root; external runtime/differential proof is not established in this inspection context, but absence of evidence is not itself a blocked integration state.");
    }
}
