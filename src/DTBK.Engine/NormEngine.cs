using DTBK.Domain;
using DTBK.Policy;

namespace DTBK.Engine;

public sealed class NormEngine
{
    private readonly INormProvider _norms;
    private readonly ILaborPriceProvider? _labor;
    private readonly IResourcePriceProvider? _resourcePrices;
    public NormEngine(INormProvider norms, ILaborPriceProvider? labor = null, IResourcePriceProvider? resourcePrices = null){_norms=norms??throw new ArgumentNullException(nameof(norms));_labor=labor;_resourcePrices=resourcePrices;}
    public IReadOnlyList<ResourceConsumption> CalculateConsumptionWithPrice(string normCode,decimal quantity,int provinceId,DateTime priceDate,decimal adjustmentFactor=1.0m,RoundingPolicy? rounding=null,decimal auxiliaryLaborPercent=0m)
    {
        if(quantity<0)throw new ArgumentOutOfRangeException(nameof(quantity));if(adjustmentFactor<=0)throw new ArgumentOutOfRangeException(nameof(adjustmentFactor));
        var norm=_norms.GetNorm(normCode)??throw new InvalidOperationException($"Không tìm thấy ĐM: {normCode}");var details=_norms.GetNormDetails(norm.NormID);var result=new List<ResourceConsumption>();decimal totalMainMaterial=0m;decimal totalMainLabor=0m;
        foreach(var d in details){if(d.Percentage.HasValue&&d.ResourceType==ResourceType.Material)continue;var qty=Math.Round(d.Quantity*quantity*adjustmentFactor,6);decimal? unitPrice=null;decimal? amount=null;
            if(d.ResourceType==ResourceType.Labor&&_labor!=null&&!string.IsNullOrWhiteSpace(d.LaborGroup)){try{var lp=_labor.ResolvePrice(d.LaborGroup,provinceId,priceDate);unitPrice=lp.UnitPrice;}catch{}}
            else if(d.ResourceType is ResourceType.Material or ResourceType.Machine&&_resourcePrices!=null){try{unitPrice=_resourcePrices.ResolveUnitPrice(d.ResourceType,d.ResourceCode,provinceId,priceDate);}catch{}}
            if(unitPrice.HasValue){amount=Round(qty*unitPrice.Value,rounding);if(d.ResourceType==ResourceType.Material)totalMainMaterial+=amount.Value;if(d.ResourceType==ResourceType.Labor)totalMainLabor+=amount.Value;}
            result.Add(new ResourceConsumption(d.ResourceCode,d.ResourceName,d.ResourceType,d.Unit,qty,d.LaborGroup,unitPrice,amount,false,null,$"NormDetailID={d.NormDetailID}"));}
        foreach(var d in details.Where(x=>x.Percentage.HasValue&&x.ResourceType==ResourceType.Material)){var pct=d.Percentage!.Value;if(pct<0)throw new InvalidOperationException($"Tỷ lệ VL khác âm: {d.ResourceCode}");var other=Round(totalMainMaterial*pct/100m,rounding);result.Add(new ResourceConsumption(d.ResourceCode,d.ResourceName,ResourceType.Material,"%",pct,"",null,other,true,pct,$"NormDetailID={d.NormDetailID}"));}
        if(auxiliaryLaborPercent>0&&totalMainLabor>0){var aux=Round(totalMainLabor*auxiliaryLaborPercent/100m,rounding);result.Add(new ResourceConsumption("NC_PHU_TRO","Nhân công phụ trợ",ResourceType.Labor,"%",auxiliaryLaborPercent,"",null,aux,false,auxiliaryLaborPercent,"AuxiliaryLabor"));}
        return result;
    }
    static decimal Round(decimal value,RoundingPolicy? policy)=>policy is null?Math.Round(value,0,MidpointRounding.AwayFromZero):RoundingPolicyEngine.RoundCurrency(value,policy);
}
