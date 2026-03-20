// File    : I18nRecord.cs
// Module  : Data
// Layer   : Core
// Purpose : POCO i18n resource từ Sys_Resource (pivoted).

namespace ConfigStudio.WPF.UI.Core.Data;

public sealed class I18nRecord
{
    public string ResourceKey { get; init; } = "";
    public string? ViVn { get; init; }
    public string? EnUs { get; init; }
    public string? JaJp { get; init; }
}

public sealed class LanguageRecord
{
    public string LangCode { get; init; } = "";
    public string LangName { get; init; } = "";
    public bool IsDefault { get; init; }
}
