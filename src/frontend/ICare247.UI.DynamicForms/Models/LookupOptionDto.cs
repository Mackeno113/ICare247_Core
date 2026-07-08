// File    : LookupOptionDto.cs
// Module  : ICare247.UI.DynamicForms
// Purpose : Một option trong danh sách select — map từ Sys_Lookup item. Dùng chung host + renderer.

namespace ICare247.UI.DynamicForms.Models;

/// <summary>Một option trong danh sách select — map từ Sys_Lookup item.</summary>
public sealed class LookupOptionDto
{
    public string ItemCode  { get; set; } = "";
    public string Label     { get; set; } = "";
    public int    SortOrder { get; set; }

    /// <summary>
    /// Override để DxComboBox render đúng text trong dropdown list
    /// khi TextField reflection không áp dụng được.
    /// </summary>
    public override string ToString() => Label;
}
