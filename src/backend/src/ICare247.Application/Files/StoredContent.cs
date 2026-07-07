// File    : StoredContent.cs
// Module  : Files
// Layer   : Application
// Purpose : Kết quả một IFileStore lưu nội dung — mô tả NỘI DUNG NẰM Ở ĐÂU để repository ghi TT_TepBlob.
//           Db → bytes trong Content; FileSystem/Object → Storage_Key (path/object key tương đối).

namespace ICare247.Application.Files;

/// <summary>
/// Mô tả nơi lưu một khối nội dung sau khi <see cref="Interfaces.IFileStore"/> ghi xong.
/// Repository dùng thông tin này để chèn dòng <c>TT_TepBlob</c> (bytes hoặc key, không cả hai).
/// </summary>
/// <param name="StorageKind">Backend đã lưu: Db | FileSystem | Object.</param>
/// <param name="StorageKey">Key/path TƯƠNG ĐỐI (FileSystem/Object); <c>null</c> khi Db.</param>
/// <param name="Content">Bytes nội dung (Db); <c>null</c> khi FileSystem/Object (đã nằm ngoài DB).</param>
/// <param name="SizeBytes">Kích thước thật của nội dung (bytes).</param>
public sealed record StoredContent(
    string StorageKind, string? StorageKey, byte[]? Content, long SizeBytes);

/// <summary>Kết quả kiểm tra sức khỏe một backend lưu trữ (dùng fail-fast lúc khởi động).</summary>
/// <param name="IsHealthy">Backend đọc/ghi được không.</param>
/// <param name="Detail">Mô tả trạng thái / lý do lỗi (log).</param>
public sealed record FileStoreHealth(bool IsHealthy, string Detail);
