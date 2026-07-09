// File    : DocTemplateListItems.cs
// Module  : Core
// Layer   : Shared
// Purpose : DTO danh sách bộ mẫu / mảnh detail cho picker màn soạn template.

namespace ConfigStudio.WPF.UI.Core.Data;

/// <summary>1 bộ mẫu (Doc_Template) trong picker.</summary>
public sealed record DocTemplateListItem(long Id, string Ma, string Ten)
{
    public override string ToString() => $"{Ma} — {Ten}";
}

/// <summary>1 mảnh detail (Doc_Template_Detail) trong picker.</summary>
public sealed record DocDetailListItem(long Id, string Ma, string Ten)
{
    public override string ToString() => $"{Ma} — {Ten}";
}
