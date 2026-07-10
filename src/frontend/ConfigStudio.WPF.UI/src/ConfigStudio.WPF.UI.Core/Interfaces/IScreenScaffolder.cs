// File    : IScreenScaffolder.cs
// Module  : Interfaces
// Layer   : Core
// Purpose : Sinh nhanh (headless) 1 màn Form nhập liệu HOẶC 1 Lưới/View từ 1 bảng Sys_Table —
//           đọc cột thật từ Target DB, ghi thẳng Config DB (Ui_Form/Ui_Field, Ui_View/Ui_View_Column).
//           Dùng cho 2 nút "Sinh Form" / "Sinh Lưới" trên màn Sys_Table. Form ↔ View độc lập,
//           không ép đi kèm nhau.

namespace ConfigStudio.WPF.UI.Core.Interfaces;

/// <summary>
/// Kết quả một lần sinh nhanh màn hình.
/// </summary>
/// <param name="Success">Sinh thành công hay không.</param>
/// <param name="GeneratedId">Form_Id / View_Id vừa tạo (0 nếu thất bại).</param>
/// <param name="ItemCount">Số field (Form) hoặc số cột (View) đã sinh.</param>
/// <param name="Message">Thông điệp hiển thị cho người dùng (thành công hoặc lý do lỗi).</param>
public readonly record struct ScaffoldResult(bool Success, int GeneratedId, int ItemCount, string Message);

/// <summary>
/// Dịch vụ sinh nhanh Form / Lưới từ 1 bảng. Mỗi method là 1 hành động độc lập (1-chạm):
/// đọc cột từ Target DB (bỏ PK/Identity), tự tạo Sys_Column nếu thiếu, ghi cấu hình Config DB.
/// Phạm vi hiện tại: 1 màn / 1 lưới đơn giản (chưa master-detail).
/// </summary>
public interface IScreenScaffolder
{
    /// <summary>
    /// Sinh 1 <c>Ui_Form</c> (1 section) + toàn bộ <c>Ui_Field</c> từ cột của bảng.
    /// Control suy theo kiểu dữ liệu SQL; cột FK (theo Sys_Relation) → LookupBox dynamic.
    /// Trả về <see cref="ScaffoldResult"/> — không ném cho lỗi nghiệp vụ thường gặp
    /// (chưa cấu hình Target DB, bảng không có cột), chỉ set Success=false + Message.
    /// </summary>
    Task<ScaffoldResult> GenerateFormAsync(
        int tableId, string schemaName, string tableCode, string tableName,
        int tenantId, CancellationToken ct = default);

    /// <summary>
    /// Sinh 1 <c>Ui_View</c> (Grid) + toàn bộ <c>Ui_View_Column</c> từ cột của bảng.
    /// Bật sẵn dòng lọc + column-chooser + thêm/sửa/xóa mặc định. 1 lưới đơn, chưa master-detail.
    /// </summary>
    Task<ScaffoldResult> GenerateGridAsync(
        int tableId, string schemaName, string tableCode, string tableName,
        int tenantId, CancellationToken ct = default);

    /// <summary>
    /// Kiểm tra bảng đã có <c>Ui_View</c> (active) chưa — khớp theo <c>Table_Id</c> HOẶC
    /// <c>View_Code</c> = <paramref name="tableCode"/>. Dùng để ẩn nút "Sinh Lưới" khi đã có.
    /// </summary>
    Task<bool> ViewExistsForTableAsync(
        int tableId, string tableCode, int tenantId, CancellationToken ct = default);
}
