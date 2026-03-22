// File    : LookupItem.cs
// Module  : Lookup
// Layer   : Domain
// Purpose : Entity đại diện cho một mục trong Sys_Lookup (danh mục dùng chung).

namespace ICare247.Domain.Entities.Lookup;

/// <summary>
/// Một mục trong danh mục Sys_Lookup.
/// <para>
/// <c>ItemCode</c> là giá trị lưu vào cột nghiệp vụ (nvarchar).
/// <c>LabelKey</c> trỏ vào Sys_Resource để resolve text theo ngôn ngữ.
/// </para>
/// </summary>
public sealed class LookupItem
{
    public int    LookupId    { get; init; }
    public int    TenantId    { get; init; }
    public string LookupCode  { get; init; } = "";
    public string ItemCode    { get; init; } = "";
    public string LabelKey    { get; init; } = "";
    public string Label       { get; init; } = ""; // Resolved từ Sys_Resource
    public int    SortOrder   { get; init; }
    public bool   IsActive    { get; init; } = true;
}
