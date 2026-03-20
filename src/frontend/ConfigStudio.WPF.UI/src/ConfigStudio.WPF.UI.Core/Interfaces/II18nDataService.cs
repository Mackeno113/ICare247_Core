// File    : II18nDataService.cs
// Module  : Data
// Layer   : Core
// Purpose : Interface CRUD Sys_Resource + Sys_Language cho I18nManagerView.

using ConfigStudio.WPF.UI.Core.Data;

namespace ConfigStudio.WPF.UI.Core.Interfaces;

/// <summary>
/// CRUD i18n resources (Sys_Resource) + languages (Sys_Language).
/// Global — không có Tenant_Id.
/// </summary>
public interface II18nDataService
{
    Task<IReadOnlyList<I18nRecord>> GetResourcesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<LanguageRecord>> GetLanguagesAsync(CancellationToken ct = default);
    Task<string?> ResolveKeyAsync(string resourceKey, string langCode, CancellationToken ct = default);
    Task SaveResourceAsync(string resourceKey, string langCode, string value, CancellationToken ct = default);
    Task DeleteResourceAsync(string resourceKey, CancellationToken ct = default);
}
