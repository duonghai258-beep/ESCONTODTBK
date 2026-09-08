namespace DTBK.Engine;

/// <summary>
/// Evidence-backed ESCON 24-sheet surface contract. Unresolved private UI
/// bindings remain explicit nulls instead of being guessed.
/// </summary>
public sealed record EsconSheetParity(
    int Order,
    string Name,
    string? DataType,
    string? PrimaryKey,
    IReadOnlyList<string> KnownGroups,
    IReadOnlyList<string> KnownControls);

public static class Escon24SheetParityRegistry
{
    public static IReadOnlyList<EsconSheetParity> Sheets { get; } = new[]
    {
        S(1,"TIÊN LƯỢNG","CongTac","ID",new[]{"KÍCH THƯỚC","ĐƠN GIÁ","THÀNH TIỀN","HỆ SỐ ĐIỀU CHỈNH"},new[]{"Phương pháp lập dự toán","Dự toán phát sinh","Chèn dòng","Xóa dòng","Ẩn/Hiện dòng, cột","Lựa chọn bộ đơn giá"}),
        S(2,"GIÁ VẬT TƯ","VatTu","ID",new[]{"GIÁ VẬT TƯ"}),
        S(3,"CƯỚC VCCG","VatLieu","ID",new[]{"BỐC XẾP","VẬN CHUYỂN"}),
        S(4,"CƯỚC VCTC",null,null), S(5,"CƯỚC VCDT",null,null), S(6,"CP TRUNG CHUYỂN",null,null),
        S(7,"GIÁ NHÂN CÔNG",null,null), S(8,"NCLM GỐC",null,null), S(9,"NCLM HIỆN TẠI",null,null),
        S(10,"PT MÁY",null,null), S(11,"TH MÁY",null,null), S(12,"PT BÙ GIÁ MÁY",null,null),
        S(13,"TH BÙ GIÁ MÁY",null,null), S(14,"PT VẬT TƯ",null,null), S(15,"TH VẬT TƯ",null,null),
        S(16,"NHIÊN LIỆU, NCLM",null,null), S(17,"DG CÔNG TRÌNH",null,null), S(18,"THKPHM",null,null),
        S(19,"CP THIẾT BỊ",null,null), S(20,"CP XÂY DỰNG",null,null), S(21,"HẠNG MỤC CHUNG",null,null),
        S(22,"DỰ PHÒNG PHÍ",null,null), S(23,"TH KINH PHÍ",null,null), S(24,"BÌA DỰ TOÁN",null,null)
    };

    public static bool IsCompleteOrder(IReadOnlyList<EsconSheetParity> sheets)
        => sheets.Count == 24 && sheets.Select(x => x.Order).SequenceEqual(Enumerable.Range(1, 24));

    private static EsconSheetParity S(int order,string name,string? dataType=null,string? key=null,string[]? groups=null,string[]? controls=null)
        => new(order,name,dataType,key,groups ?? Array.Empty<string>(),controls ?? Array.Empty<string>());
}
