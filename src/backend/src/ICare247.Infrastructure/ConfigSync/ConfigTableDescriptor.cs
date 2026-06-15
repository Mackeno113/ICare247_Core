// File    : ConfigTableDescriptor.cs
// Module  : ConfigSync
// Layer   : Infrastructure
// Purpose : Mô tả một bảng config cho engine đồng bộ generic (CFGSYNC-2) — khóa tự nhiên,
//           parent FK cần re-link theo mã, các cột cờ. Engine dùng mô tả này để UPSERT
//           mà không hardcode logic riêng từng bảng.

namespace ICare247.Infrastructure.ConfigSync;

/// <summary>
/// Liên kết tới một bảng cha qua một cột FK — phục vụ re-link FK theo MÃ (spec §3).
/// Engine dịch giá trị FK của master sang Id tương ứng ở tenant qua "khóa nghiệp vụ" của cha.
/// </summary>
/// <param name="FkColumn">Cột FK trên bảng con (vd "Form_Id", "Column_Id").</param>
/// <param name="ParentTable">Tên bảng cha (vd "Ui_Form") — phải được đồng bộ TRƯỚC bảng con.</param>
internal sealed record ParentLink(string FkColumn, string ParentTable);

/// <summary>
/// Mô tả đồng bộ cho một bảng config. Thứ tự xuất hiện trong <see cref="ConfigSyncTables.Order"/>
/// chính là thứ tự phụ thuộc (cha trước con).
/// </summary>
internal sealed class ConfigTableDescriptor
{
    /// <summary>Tên bảng (không schema), vd "Ui_Field".</summary>
    public required string TableName { get; init; }

    /// <summary>Cột khóa chính IDENTITY (vd "Field_Id") — KHÔNG bê sang tenant (mỗi DB khác Id).</summary>
    public required string IdColumn { get; init; }

    /// <summary>
    /// Cột mã nghiệp vụ định danh dòng TRONG ngữ cảnh cha (vd "Field_Code", "Table_Code").
    /// Kết hợp với khóa nghiệp vụ của <see cref="ContextParent"/> tạo "khóa nghiệp vụ" toàn cục.
    /// </summary>
    public required string LocalKeyColumn { get; init; }

    /// <summary>
    /// Cha tạo ngữ cảnh cho khóa (vd Ui_Field thuộc Ui_Form → Field_Code chỉ unique trong form).
    /// <c>null</c> nếu mã đã unique toàn cục (vd Table_Code, Form_Code).
    /// </summary>
    public ParentLink? ContextParent { get; init; }

    /// <summary>
    /// Mọi cột FK cần re-link sang Id tenant (gồm cả <see cref="ContextParent"/> nếu có +
    /// các FK chỉ-tham-chiếu, vd Ui_Field.Column_Id → Sys_Column, Ui_Field.Section_Id → Ui_Section).
    /// </summary>
    public IReadOnlyList<ParentLink> RelinkParents { get; init; } = [];

    /// <summary>
    /// Tên cột tombstone (Is_Active). <c>null</c> nếu bảng không có cột này (vd Ui_Field dùng Is_Visible,
    /// không tombstone được) → engine bỏ qua bước ngừng kích hoạt cho bảng đó.
    /// </summary>
    public string? ActiveColumn { get; init; } = "Is_Active";

    /// <summary>Tên cột version nguồn (vd "Version"). <c>null</c> nếu bảng không có → Source_Ver để null.</summary>
    public string? VersionColumn { get; init; } = "Version";
}
