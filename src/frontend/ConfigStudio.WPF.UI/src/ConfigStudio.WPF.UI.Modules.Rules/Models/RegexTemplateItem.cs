// File    : RegexTemplateItem.cs
// Module  : Rules
// Layer   : Presentation
// Purpose : Model cho một mẫu regex có sẵn — hiển thị dạng chip trong Edit Panel.

namespace ConfigStudio.WPF.UI.Modules.Rules.Models;

/// <summary>Một mẫu regex có sẵn — user click chip để áp dụng ngay vào ô pattern.</summary>
public record RegexTemplateItem(string Label, string Pattern);
