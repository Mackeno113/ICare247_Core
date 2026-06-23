// File    : ISchemaMaintenanceService.cs
// Module  : Interfaces
// Layer   : Core
// Purpose : Thực thi thay đổi cấu trúc (DDL) lên Target DB — đối ứng ghi của
//           ISchemaInspectorService (chỉ đọc). Dùng cho nút "Kiểm tra cột chuẩn".

namespace ConfigStudio.WPF.UI.Core.Interfaces;

/// <summary>
/// Dịch vụ bảo trì schema Target DB: thực thi các câu lệnh ALTER idempotent.
/// </summary>
public interface ISchemaMaintenanceService
{
    /// <summary>
    /// Thực thi tuần tự các câu lệnh ALTER (mỗi câu là 1 batch tự bọc IF COL_LENGTH IS NULL)
    /// lên Target DB. Idempotent — chỉ tác động cột chưa có. Trả số câu lệnh đã chạy.
    /// </summary>
    /// <param name="connectionString">Connection string tới Target DB.</param>
    /// <param name="statements">Các batch T-SQL (KHÔNG chứa 'GO').</param>
    /// <param name="ct">Cancellation token.</param>
    Task<int> ExecuteStatementsAsync(
        string connectionString,
        IReadOnlyList<string> statements,
        CancellationToken ct = default);
}
