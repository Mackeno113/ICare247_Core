// File    : IFileStoreSelector.cs
// Module  : Files
// Layer   : Application
// Purpose : Định tuyến chọn IFileStore đúng: theo KÍCH THƯỚC khi ghi (nhỏ→Db, lớn→provider cấu hình)
//           và theo Storage_Kind đã lưu khi đọc/xóa. Che giấu chính sách routing khỏi handler/repository.

namespace ICare247.Application.Interfaces;

/// <summary>
/// Bộ chọn backend lưu trữ. Ghi: file ≤ ngưỡng → Db, lớn hơn → provider đã cấu hình
/// (FileSystem/Object; hoặc Db nếu deployment không có shared storage). Đọc/xóa: theo Storage_Kind đã ghi.
/// </summary>
public interface IFileStoreSelector
{
    /// <summary>Chọn store để GHI theo kích thước nội dung.</summary>
    /// <param name="sizeBytes">Kích thước nội dung (bytes).</param>
    /// <returns>Db khi ≤ ngưỡng; ngược lại là store của provider cấu hình.</returns>
    /// <remarks>Không side-effect — chỉ trả instance đã đăng ký DI.</remarks>
    IFileStore SelectForSize(long sizeBytes);

    /// <summary>Chọn store để ĐỌC/XÓA theo Storage_Kind đã lưu ở TT_TepBlob.</summary>
    /// <param name="storageKind">Db | FileSystem | Object.</param>
    /// <returns>Store tương ứng.</returns>
    /// <exception cref="System.NotSupportedException">Storage_Kind không nhận diện được.</exception>
    IFileStore SelectForKind(string storageKind);
}
