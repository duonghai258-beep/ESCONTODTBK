namespace DTBK.Domain;

// Minimal engine-only contract surface extracted from DTBK.Domain.
// UI, reporting, persistence, vendor adapters and legal documents are intentionally excluded.
public enum VerificationStatus { Verified, NeedsVerification, Blocked }
public enum IntegrationVerificationStatus { Verified, PartiallyVerified, Unverified, SourceOnly, Blocked }
public enum WorkType { Civil = 1, Industrial = 2, Traffic = 3, Infrastructure = 4, Irrigation = 5 }
public enum LocationType { Normal = 1, Mountain = 2, Border = 3, Offshore = 4, Island = 5 }
public enum ResourceType { Material = 1, Labor = 2, Machine = 3 }

public sealed record CalculationContext(string ProjectId,string ProjectName,string PackageName,string ProvinceCode,string LocationCode,DateTime AsOfDate,string Investor,string Estimator,string DataPackVersion,string LegalRuleVersion);
public sealed record CalculationProvenance(string SourceId,string DocumentNo,string RuleId,DateTime? EffectiveFrom,DateTime? EffectiveTo,string Applicability,string FormulaMetadata,string RoundingRule,VerificationStatus VerificationStatus);
public sealed record LaborPrice(decimal UnitPrice);
public sealed record CoefficientRule(string Code, decimal Factor);
public sealed record CoefficientSet(IReadOnlyList<CoefficientRule> Rules);

public interface INormProvider { object? GetNorm(string normCode); }
public interface IResourcePriceProvider { decimal ResolveUnitPrice(ResourceType type,string resourceCode,int provinceId,DateTime date); }
public interface ILaborPriceProvider { LaborPrice ResolvePrice(string groupCode,int provinceId,DateTime date); }
public interface ICoefficientProvider { CoefficientSet Resolve(WorkType workType,LocationType location,DateTime effectiveDate,IEnumerable<string>? extraCodes=null); }
