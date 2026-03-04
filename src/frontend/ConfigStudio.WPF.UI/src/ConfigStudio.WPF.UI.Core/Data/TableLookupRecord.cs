// File    : TableLookupRecord.cs
// Module  : Data
// Layer   : Core
// Purpose : POCO cho danh sách tra cứu Sys_Table khi chọn khóa ngoại Table_Id.

namespace ConfigStudio.WPF.UI.Core.Data;

/// <summary>
/// Bản ghi tra cứu bảng metadata từ <c>dbo.Sys_Table</c>.
/// </summary>
public sealed class TableLookupRecord
{
    public int TableId { get; init; }
    public string TableCode { get; init; } = "";
    public string TableName { get; init; } = "";
    public string SchemaName { get; init; } = "";
    public string Description { get; init; } = "";

    /// <summary>
    /// Chuỗi hiển thị trong SearchLookUpEdit.
    /// </summary>
    public string DisplayText => $"{TableCode} - {TableName} (Id: {TableId})";
}
