namespace DTBK.Engine;

/// <summary>Evidence-backed ESCON formula contracts used by parity tests.</summary>
public static class EsconFormulaParityContract
{
    public static IReadOnlyDictionary<string,string> Confirmed { get; } = new Dictionary<string,string>(StringComparer.Ordinal)
    {
        ["TIÊN LƯỢNG.KLPhu"] = "PRODUCT([SoCK];[KTDai];[KTRong];[KTCao];[HSPhu])",
        ["TIÊN LƯỢNG.TTDGVL"] = "[DGVL]*[KhoiLuong]",
        ["TIÊN LƯỢNG.TTDGVLP"] = "[DGVLP]*[KhoiLuong]",
        ["TIÊN LƯỢNG.TTDGNC"] = "[DGNC]*[KhoiLuong]",
        ["TIÊN LƯỢNG.TTDGCM"] = "[DGCM]*[KhoiLuong]",
        ["GIÁ VẬT TƯ.GiaHT"] = "[GiaTB]*[HSDC]/(1+[VAT])+[CuocVCCG]+[CuocVCTC]+[CuocVCDT]+[CPTCBQ]",
        ["CƯỚC VCCG.GiaCuocSDC"] = "[GiaCuoc]/(1+SPVALUE(VAT))*(1+[TyLeDieuChinh])",
        ["CƯỚC VCCG.ThanhTienVC"] = "[HSPhuongTien]*[CuLyTinhCuoc]*[GiaCuocSDC]",
        ["CƯỚC VCCG.TongCuoc"] = "[TyTrong]*[HSBacHang]*SUMGROUP([ThanhTienVC])+[CuocBS]+[CuocKhac]",
        ["CƯỚC VCCG.ThanhTienBX"] = "[TyTrong]*([DinhMucBX]*[LuongNCBX]+[CuocBX])",
        ["CƯỚC VCCG.TongTienBX"] = "SUMGROUP([ThanhTienBX])",
        ["QUYẾT TOÁN.DGTC"] = "[DGVL]+[DGVLP]+[DGNC]+[DGCM]",
        ["QUYẾT TOÁN.ThanhTien"] = "[KhoiLuong]*[DGTC]"
    };

    public static IReadOnlyList<string> RequiredSpecialFunctions { get; } = new[] { "SPVALUE", "SUMGROUP", "PRODUCT", "IF", "OR", "AND" };
}
