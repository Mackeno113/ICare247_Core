// File    : IAuditDbConnectionFactory.cs
// Module  : Common
// Layer   : Application
// Purpose : Factory tạo connection tới Audit DB (NK_* — nhật ký hoạt động/lỗi) của tenant.
//           Tách biệt IDataDbConnectionFactory (Data DB nghiệp vụ) dù thường trỏ chung DB
//           khi tenant chưa tách Audit DB riêng (xem ITenantContext.AuditConnectionString).

using System.Data;

namespace ICare247.Application.Interfaces;

/// <summary>Factory tạo <see cref="IDbConnection"/> cho Audit DB — đọc NK_NhatKyHoatDong/NK_LoiHeThong.</summary>
public interface IAuditDbConnectionFactory
{
    /// <summary>Tạo connection mới tới Audit DB.</summary>
    IDbConnection CreateConnection();
}
