// File    : IAppConfigService.cs
// Module  : Infrastructure
// Layer   : Core
// Purpose : Đọc cấu hình kết nối DB từ file ngoài source code (bảo mật).

namespace ConfigStudio.WPF.UI.Core.Interfaces;

/// <summary>
/// Dịch vụ đọc cấu hình ứng dụng từ
/// <c>%APPDATA%\ICare247\ConfigStudio\appsettings.json</c>.
/// File này KHÔNG commit vào git — mỗi môi trường tự tạo riêng.
/// </summary>
public interface IAppConfigService
{
    /// <summary>Connection string tới Config DB (Ui_Form, Ui_Field, ...). Null nếu chưa cấu hình.</summary>
    string? ConnectionString { get; }

    /// <summary>
    /// Connection string tới Target DB (DB thực sự của ứng dụng — dùng để đọc cấu trúc cột).
    /// Null nếu chưa cấu hình.
    /// </summary>
    string? TargetConnectionString { get; }

    /// <summary>Tenant_Id mặc định cho phiên làm việc này.</summary>
    int TenantId { get; }

    /// <summary>True nếu ConnectionString đã được load và không rỗng.</summary>
    bool IsConfigured { get; }

    /// <summary>True nếu TargetConnectionString đã được cấu hình.</summary>
    bool IsTargetConfigured { get; }

    /// <summary>Đường dẫn đầy đủ tới file cấu hình trên máy.</summary>
    string ConfigFilePath { get; }

    /// <summary>
    /// Load cấu hình từ file. Nếu file chưa tồn tại → tạo template và trả về false.
    /// </summary>
    Task<bool> LoadAsync();

    /// <summary>
    /// Kiểm tra kết nối DB với connection string cho trước.
    /// Trả về <c>null</c> nếu thành công; message lỗi nếu thất bại.
    /// </summary>
    Task<string?> TestConnectionAsync(string connectionString);

    /// <summary>
    /// Ghi toàn bộ cấu hình vào file và cập nhật state hiện tại.
    /// </summary>
    Task SaveAsync(string connectionString, int tenantId, string? targetConnectionString = null);
}
