// File    : IDocTemplateDataService.cs
// Module  : Core
// Layer   : Shared
// Purpose : CRUD bộ mẫu tài liệu (Doc_Template*) trên Config DB + nạp/lưu bytes fragment .docx.
// Spec    : docs/spec/28_DOC_TEMPLATE_SPEC.md §4, §8.

using ConfigStudio.WPF.UI.Core.Data;

namespace ConfigStudio.WPF.UI.Core.Interfaces;

/// <summary>Truy cập cấu hình bộ mẫu tài liệu (Config DB). ConfigStudio nối thẳng DB.</summary>
public interface IDocTemplateDataService
{
    /// <summary>Danh sách bộ mẫu (tenant hiện tại).</summary>
    Task<IReadOnlyList<DocTemplateListItem>> GetTemplatesAsync(CancellationToken ct = default);

    /// <summary>Tạo bộ mẫu mới (master). Trả Id mới. Sự kiện theo sau: có thể soạn fragment master ngay.</summary>
    Task<long> CreateTemplateAsync(string ma, string ten, string masterProc, CancellationToken ct = default);

    /// <summary>Danh sách mảnh detail của 1 bộ mẫu (sắp theo Thu_Tu).</summary>
    Task<IReadOnlyList<DocDetailListItem>> GetDetailsAsync(long templateId, CancellationToken ct = default);

    /// <summary>Thêm 1 mảnh detail. Trả Id mới.</summary>
    Task<long> CreateDetailAsync(
        long templateId, string ma, string ten, string detailProc, int thuTu, CancellationToken ct = default);

    /// <summary>Nạp bytes fragment master (null nếu chưa soạn).</summary>
    Task<byte[]?> GetMasterDocxAsync(long templateId, CancellationToken ct = default);

    /// <summary>Lưu bytes fragment master.</summary>
    Task SaveMasterDocxAsync(long templateId, byte[] docx, CancellationToken ct = default);

    /// <summary>Nạp bytes fragment 1 mảnh detail (null nếu chưa soạn).</summary>
    Task<byte[]?> GetDetailDocxAsync(long detailId, CancellationToken ct = default);

    /// <summary>Lưu bytes fragment 1 mảnh detail.</summary>
    Task SaveDetailDocxAsync(long detailId, byte[] docx, CancellationToken ct = default);
}
