// File    : SysTableRecord.cs
// Module  : Data
// Layer   : Core
// Purpose : POCO map dữ liệu cấu hình bảng từ Sys_Table cho màn hình nhập liệu.

namespace ConfigStudio.WPF.UI.Core.Data;

/// <summary>
/// Bản ghi cấu hình bảng trong <c>dbo.Sys_Table</c>.
/// </summary>
public sealed class SysTableRecord
{
    public int TableId { get; init; }
    public string TableCode { get; init; } = "";
    public string TableName { get; init; } = "";
    public string SchemaName { get; init; } = "";
    public bool IsTenant { get; init; }
    public int TenantId { get; init; }
    public int Version { get; init; }
    public string Checksum { get; init; } = "";
    public bool IsActive { get; init; }
    public DateTime? CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public string Description { get; init; } = "";
}
