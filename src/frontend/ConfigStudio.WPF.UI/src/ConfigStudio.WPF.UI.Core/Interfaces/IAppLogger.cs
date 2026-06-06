// File    : IAppLogger.cs
// Module  : Core
// Layer   : Abstraction
// Purpose : Cổng ghi log dùng chung cho toàn app WPF. Trừu tượng hóa Serilog
//           để ViewModel/Service không phụ thuộc trực tiếp logging framework.

namespace ConfigStudio.WPF.UI.Core.Interfaces;

/// <summary>
/// Cổng ghi log của ConfigStudio. Implementation (SerilogAppLogger) tự phân loại
/// exception: lỗi SQL (SqlException) → file sql-errors, lỗi C#/.NET → file app.
/// </summary>
public interface IAppLogger
{
    /// <summary>
    /// Ghi một exception. Tự nhận diện SqlException trong chuỗi inner để route
    /// sang đúng file log và đính kèm metadata SQL (Number, Procedure, Line...).
    /// </summary>
    /// <param name="ex">Exception cần ghi.</param>
    /// <param name="context">Mô tả ngữ cảnh, vd "Lưu Field DiemThiDauVao".</param>
    void Capture(Exception ex, string? context = null);

    /// <summary>Ghi thông tin chẩn đoán (không phải lỗi) vào file app.</summary>
    void Info(string message, string? context = null);

    /// <summary>Đẩy buffer log xuống đĩa — gọi khi app thoát.</summary>
    void Flush();
}
