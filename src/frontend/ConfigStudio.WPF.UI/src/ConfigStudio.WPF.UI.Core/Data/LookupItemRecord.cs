// File    : LookupItemRecord.cs
// Module  : Core
// Layer   : Data
// Purpose : POCO đại diện một item từ Sys_Lookup — dùng trong WPF để render RadioGroup/ComboBox.

namespace ConfigStudio.WPF.UI.Core.Data;

/// <summary>
/// Một mục trong danh mục Sys_Lookup.
/// <c>ItemCode</c> là giá trị lưu vào DB.
/// <c>Label</c> là text hiển thị đã resolve theo ngôn ngữ.
/// </summary>
public sealed class LookupItemRecord
{
    public string ItemCode  { get; init; } = "";
    public string Label     { get; init; } = "";
    public string LabelKey  { get; init; } = "";
    public int    SortOrder { get; init; }
}
