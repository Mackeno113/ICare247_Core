// File    : IStorageKeyBuilder.cs
// Module  : Files
// Layer   : Application
// Purpose : Dựng KEY TƯƠNG ĐỐI, ổn định, sinh từ dữ liệu (không phụ thuộc ổ đĩa) cho nội dung tệp.
//           Cấu trúc BẤT BIẾN khi di dời gốc chứa → chỉ BaseRoot/endpoint đổi, key trong DB giữ nguyên.

namespace ICare247.Application.Interfaces;

/// <summary>
/// Dựng Storage_Key tương đối cho nội dung tệp. Key luôn sinh ở server (chống path traversal),
/// tiền tố theo tenant để cô lập, sharding theo checksum để tránh thư mục phình + giới hạn độ dài path.
/// </summary>
public interface IStorageKeyBuilder
{
    /// <summary>
    /// Dựng key tương đối: <c>[siteKey/]{tenantId}/{yyyy}/{MM}/{loai}/{sha[0..2]}/{sha}.{ext}</c>.
    /// </summary>
    /// <param name="tenantId">Tenant sở hữu tệp (cô lập thư mục — chống đụng key giữa tenant).</param>
    /// <param name="loai">Phân loại (vd 'Logo', 'HopDong'); rỗng → 'chung'.</param>
    /// <param name="checksum">SHA256 hex của nội dung — vừa dedup vừa tên file (sharding 2 ký tự đầu).</param>
    /// <param name="fileName">Tên gốc — chỉ dùng để suy phần mở rộng an toàn.</param>
    /// <returns>Key tương đối dùng dấu <c>/</c>; an toàn ghép với BaseRoot/bucket.</returns>
    /// <remarks>Không có side-effect — hàm thuần. Không chèn thành phần do client kiểm soát vào path.</remarks>
    string Build(int tenantId, string? loai, string checksum, string fileName);
}
