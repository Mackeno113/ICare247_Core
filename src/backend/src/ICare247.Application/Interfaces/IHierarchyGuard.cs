// File    : IHierarchyGuard.cs
// Module  : MasterData
// Layer   : Application
// Purpose : Bảo vệ toàn vẹn CÂY khi ghi — áp dụng CHUNG cho MỌI bảng tự tham chiếu.
//           Nguyên tắc (mọi cây): 1 node KHÔNG được nhận chính nó (self-parent) hay 1 hậu duệ
//           của nó làm cha (cycle). Cột cha đọc từ Sys_Relation (Master = Detail), KHÔNG đoán
//           theo tên cột — đồng bộ chính sách "no FK inference" (ReferenceCheckService / ADR).

namespace ICare247.Application.Interfaces;

/// <summary>
/// Guard toàn vẹn cây cho đường GHI danh mục. Bảng không phải cây (không khai quan hệ tự tham chiếu
/// ở <c>Sys_Relation</c>) → không guard. Dùng chung cho mọi màn/lưới cây (công ty, phòng ban, menu…).
/// </summary>
public interface IHierarchyGuard
{
    /// <summary>
    /// Kiểm tra giá trị cột cha tự tham chiếu khi lưu 1 bản ghi. Trả vi phạm đầu tiên gặp, hoặc
    /// <c>null</c> nếu hợp lệ (bảng không phải cây · đang Insert bản ghi mới chưa có khóa · đặt cha = NULL).
    /// </summary>
    /// <param name="tableId">Table_Id (Config DB) của bảng đích — tra quan hệ tự tham chiếu.</param>
    /// <param name="values">Cột↔giá trị của payload lưu (chứa cột cha nếu form đụng tới).</param>
    Task<HierarchyViolation?> CheckSelfReferenceAsync(
        int tableId, string schemaName, string tableName, string pkColumn,
        object? id, IReadOnlyDictionary<string, object?> values, CancellationToken ct = default);
}

/// <summary>1 vi phạm toàn vẹn cây: cột cha vi phạm + loại.</summary>
public sealed record HierarchyViolation(string Column, HierarchyViolationKind Kind);

/// <summary>SelfParent = cha trỏ chính nó; Cycle = cha là hậu duệ của node (tạo vòng lặp).</summary>
public enum HierarchyViolationKind { SelfParent, Cycle }
