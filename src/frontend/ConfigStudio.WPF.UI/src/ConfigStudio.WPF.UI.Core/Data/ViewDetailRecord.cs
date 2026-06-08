// File    : ViewDetailRecord.cs
// Module  : Data
// Layer   : Core
// Purpose : Gói dữ liệu đầy đủ của một View (header + cột + action) khi nạp editor.

using System.Collections.Generic;

namespace ConfigStudio.WPF.UI.Core.Data;

/// <summary>
/// Tổng hợp chi tiết một View: header <see cref="ViewRecord"/> + danh sách cột + danh sách action.
/// Trả về khi mở một View để chỉnh sửa.
/// </summary>
public sealed class ViewDetailRecord
{
    public required ViewRecord Header { get; init; }
    public List<ViewColumnRecord> Columns { get; init; } = [];
    public List<ViewActionRecord> Actions { get; init; } = [];
}
