// File    : RelationRecord.cs
// Module  : Data
// Layer   : Core
// Purpose : POCO map header bảng Sys_Relation — registry quan hệ tường minh giữa
//           2 bảng (master-detail + soft-check FK). Alias SQL khớp tên property.

namespace ConfigStudio.WPF.UI.Core.Data;

/// <summary>
/// Bản ghi một quan hệ trong <c>dbo.Sys_Relation</c> (đã mở rộng — migration 035).
/// Dùng cho lưới danh sách + nạp editor + payload lưu.
/// </summary>
public sealed class RelationRecord
{
    /// <summary>Khóa chính Relation_Id (0 = bản ghi mới chưa lưu).</summary>
    public int RelationId { get; init; }

    /// <summary>Mã quan hệ (unique, có thể null).</summary>
    public string? RelationCode { get; init; }

    /// <summary>Bảng cha (phía "1").</summary>
    public int MasterTableId { get; init; }

    /// <summary>Table_Code bảng cha — join Sys_Table để hiển thị trên lưới.</summary>
    public string MasterTableCode { get; init; } = "";

    /// <summary>Cột khóa ở bảng cha (mặc định 'Id' theo convention Data DB).</summary>
    public string MasterKeyColumn { get; init; } = "Id";

    /// <summary>Bảng con (phía "N").</summary>
    public int DetailTableId { get; init; }

    /// <summary>Table_Code bảng con — join Sys_Table để hiển thị.</summary>
    public string DetailTableCode { get; init; } = "";

    /// <summary>Cột FK vật lý ở bảng con trỏ về master (vd NoiSinh_TinhThanhPhoID).</summary>
    public string? DetailFkColumn { get; init; }

    /// <summary>Loại quan hệ: OneToMany | OneToOne.</summary>
    public string RelationType { get; init; } = "OneToMany";

    /// <summary>Hành vi khi xóa master: Restrict | Cascade | SetNull | NoAction.</summary>
    public string OnDelete { get; init; } = "Restrict";

    /// <summary>Cột hiển thị (cho master-detail/lookup) — cột của bảng cha.</summary>
    public string? DisplayColumn { get; init; }

    /// <summary>Cột giá trị (cho master-detail/lookup) — cột của bảng cha.</summary>
    public string? ValueColumn { get; init; }

    /// <summary>Quan hệ đang dùng hay đã ẩn.</summary>
    public bool IsActive { get; init; } = true;
}
