// File    : ApiErrorLocalizer.cs
// Module  : Shared
// Layer   : Frontend (Shared)
// Purpose : Quy đổi 1 Exception (đặc biệt ApiProblemException có `code`) thành thông điệp i18n
//           thân thiện cho người dùng. Tách riêng để mọi page dùng chung 1 chỗ map code → key.

using ICare247.UI.Shared.Services.I18n;

namespace ICare247.UI.Shared.Services.Http;

/// <summary>
/// Map mã lỗi backend (<see cref="ApiProblemException.Code"/>) sang chuỗi đã i18n.
/// Code chưa biết → dùng <see cref="ApiProblemException.Detail"/>; exception thường → <c>Message</c>.
/// </summary>
public static class ApiErrorLocalizer
{
    /// <summary>
    /// Trả thông điệp lỗi đã localize cho người dùng. Sự kiện theo sau: page gán vào banner/toast.
    /// </summary>
    /// <param name="loc">Dịch vụ i18n hiện hành (fallback tiếng Việt nằm tại chỗ gọi).</param>
    /// <param name="ex">Exception bắt được (thường từ *ApiService).</param>
    public static string Describe(LocalizationService loc, Exception ex)
    {
        if (ex is not ApiProblemException pe)
            return ex.Message;

        var message = pe.Code switch
        {
            MetadataNoPrimaryKey => loc.L(
                "error.metadata.noPrimaryKey",
                "Bảng dữ liệu của danh mục này chưa có khóa chính (PRIMARY KEY) nên không thể hiển thị " +
                "hay lưu. Vui lòng liên hệ quản trị để bổ sung khóa chính cho bảng."),
            _ => pe.Detail
        };
        // Vẫn nối "Mã lỗi" để người dùng báo được, dev grep log theo correlationId.
        return ApiErrorHelper.WithErrorCode(message, pe.CorrelationId);
    }

    /// <summary>Code khớp <c>MetadataConfigurationException.NoPrimaryKey</c> phía backend.</summary>
    private const string MetadataNoPrimaryKey = "metadata.no_primary_key";
}
