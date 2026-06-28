// File    : MetadataConfigurationException.cs
// Module  : Domain
// Layer   : Domain
// Purpose : Ném khi metadata/schema của form CHƯA hợp lệ để thực thi runtime
//           (vd bảng đích chưa khai báo khóa chính). Tầng Api map sang ProblemDetails
//           kèm Code ổn định để client hiển thị thông báo i18n rõ ràng (không phải 500 chung).

namespace ICare247.Domain.Exceptions;

/// <summary>
/// Cấu hình metadata của form chưa đủ điều kiện chạy.
/// <see cref="Code"/> = mã lỗi ổn định (machine-readable) → client map sang thông báo i18n;
/// <see cref="FormCode"/> = form liên quan để chẩn đoán.
/// </summary>
public sealed class MetadataConfigurationException : Exception
{
    /// <summary>Mã lỗi: bảng đích chưa khai báo khóa chính (PRIMARY KEY).</summary>
    public const string NoPrimaryKey = "metadata.no_primary_key";

    /// <summary>Mã lỗi ổn định cho client localize (xem các hằng <c>const</c> trong lớp này).</summary>
    public string Code { get; }

    /// <summary>Mã form liên quan.</summary>
    public string FormCode { get; }

    public MetadataConfigurationException(string code, string formCode, string message)
        : base(message)
    {
        Code     = code;
        FormCode = formCode;
    }
}
