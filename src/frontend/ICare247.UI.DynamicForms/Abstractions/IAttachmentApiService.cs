// File    : IAttachmentApiService.cs
// Module  : ICare247.UI.DynamicForms
// Purpose : Ổ cắm (contract) cho AttachmentRenderer — impl AttachmentApiService ở host, nối qua DI.
//           Cho phép renderer đính kèm nằm trong RCL mà không phụ thuộc HttpClient/token của host.

using ICare247.UI.DynamicForms.Models;

namespace ICare247.UI.DynamicForms.Abstractions;

/// <summary>Wrap endpoint đính kèm tổng quát cho control AttachmentRenderer.</summary>
public interface IAttachmentApiService
{
    /// <summary>Liệt kê đính kèm của 1 record/field. Trả list rỗng nếu chưa có/ lỗi.</summary>
    Task<List<AttachmentInfoDto>> ListAsync(
        string ownerTable, long ownerId, string? fieldMa, CancellationToken ct = default);

    /// <summary>Metadata 1 đính kèm theo Id (chế độ 1-tệp/cột). Null nếu không có/lỗi.</summary>
    Task<AttachmentInfoDto?> GetInfoAsync(long id, CancellationToken ct = default);

    /// <summary>Xóa 1 đính kèm. true nếu thành công.</summary>
    Task<bool> DeleteAsync(long id, CancellationToken ct = default);

    /// <summary>Gắn loạt đính kèm treo vào record vừa tạo (đa-tệp-khi-thêm-mới). Trả số bản ghi đã gắn.</summary>
    Task<int> LinkAsync(
        string ownerTable, long ownerId, string? fieldMa, IReadOnlyList<long> ids, CancellationToken ct = default);

    /// <summary>Lấy thumbnail dưới dạng data-URL (base64) để &lt;img&gt; hiển thị mà vẫn giữ auth. Null nếu không có.</summary>
    Task<string?> GetThumbnailDataUrlAsync(long id, CancellationToken ct = default);

    /// <summary>URL tải nội dung chính (dùng cho JS fetch kèm token, không nhúng token vào query).</summary>
    string DownloadUrl(long id);

    /// <summary>Access token hiện tại (cho JS fetch tải về). Rỗng nếu chưa đăng nhập.</summary>
    string Token { get; }

    /// <summary>Giá trị header X-Tenant-Id (cho JS fetch tải về).</summary>
    string TenantIdHeader { get; }

    /// <summary>Dựng option cho JS uploader: URL tuyệt đối + token + tenant (XHR tự set header).</summary>
    AttachmentUploadOptions BuildUploadOptions(
        string? loai, string? ownerTable, long? ownerId, string? fieldMa,
        int maxDimension, double quality);
}
