// File    : ILookupTemplateDataService.cs
// Module  : Data
// Layer   : Core
// Purpose : Hợp đồng CRUD mẫu lookup dùng lại trên Config DB.

using ConfigStudio.WPF.UI.Core.Data;

namespace ConfigStudio.WPF.UI.Core.Interfaces;

/// <summary>Đọc và ghi cấu hình <c>Ui_Lookup_Template</c> bằng dữ liệu thật.</summary>
public interface ILookupTemplateDataService
{
    Task<IReadOnlyList<LookupTemplateRecord>> GetTemplatesAsync(CancellationToken ct = default);
    Task<int> SaveTemplateAsync(LookupTemplateUpsertRequest request, CancellationToken ct = default);
    Task<int> CountReferencesAsync(string templateCode, CancellationToken ct = default);
    Task DeleteTemplateAsync(int templateId, CancellationToken ct = default);
}
