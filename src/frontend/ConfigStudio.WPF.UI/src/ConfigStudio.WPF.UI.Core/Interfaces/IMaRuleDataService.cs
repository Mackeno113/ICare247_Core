// File    : IMaRuleDataService.cs
// Module  : Data
// Layer   : Core
// Purpose : Hợp đồng CRUD quy tắc sinh mã (Sys_Ma_Rule + Sys_Ma_Rule_Segment) trên Config DB.

using ConfigStudio.WPF.UI.Core.Data;

namespace ConfigStudio.WPF.UI.Core.Interfaces;

/// <summary>Đọc/ghi cấu hình sinh mã bằng dữ liệu thật (MA-6, spec 32 §8).</summary>
public interface IMaRuleDataService
{
    Task<IReadOnlyList<MaRuleRecord>> GetRulesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<MaRuleSegmentRecord>> GetSegmentsAsync(int ruleId, CancellationToken ct = default);

    /// <summary>Toàn bộ đoạn của TẤT CẢ quy tắc (1 lần) — dùng tính cột "Mã dự kiến" ở lưới danh sách, tránh N+1.</summary>
    Task<IReadOnlyList<MaRuleSegmentRecord>> GetAllSegmentsAsync(CancellationToken ct = default);
    Task<int> SaveRuleAsync(MaRuleUpsertRequest request, CancellationToken ct = default);
    Task DeleteRuleAsync(int ruleId, CancellationToken ct = default);

    /// <summary>Danh sách bảng (Sys_Table) để chọn bảng đích / bảng tra LOOKUP.</summary>
    Task<IReadOnlyList<TableLookupRecord>> GetTablesAsync(CancellationToken ct = default);

    /// <summary>Cột (Sys_Column) theo mã bảng — cho combo cột đích / cột nguồn / cột LOOKUP.</summary>
    Task<IReadOnlyList<string>> GetColumnsAsync(string tableCode, CancellationToken ct = default);

    /// <summary>
    /// Đếm event SET_VALUE/CLEAR_VALUE đang nhắm vào (Table_Code, Column_Code) — cảnh báo chồng
    /// cơ chế (spec §10.1). Best-effort: schema Evt_* thiếu → trả 0, KHÔNG ném.
    /// </summary>
    Task<int> CountOverlappingEventsAsync(string tableCode, string columnCode, CancellationToken ct = default);
}
