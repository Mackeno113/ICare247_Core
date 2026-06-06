// File    : IReferenceCheckService.cs
// Module  : MasterData
// Layer   : Application
// Purpose : Soft-check tham chiếu khóa ngoại theo quy ước đặt tên (DB không có FK vật lý).
//           Trước khi xóa cứng 1 bản ghi danh mục, quét xem giá trị PK có đang được dùng
//           ở bảng khác không (cột trùng tên PK hoặc có hậu tố _<PK>).

namespace ICare247.Application.Interfaces;

/// <summary>
/// Dịch vụ kiểm tra tham chiếu "mềm" — thay cho khóa ngoại vật lý (DB không khai báo FK).
/// </summary>
/// <remarks>
/// Quy ước: PK <c>CongTyID</c> → cột tham chiếu hợp lệ là <c>CongTyID</c> (trùng) hoặc
/// <c>*_CongTyID</c> (hậu tố, vd <c>ChiNhanh_CongTyID</c>).
/// <para>
/// Quét <c>Sys_Column</c> <b>KHÔNG lọc Is_Active</b> — bắt cả metadata đã deactivate vì
/// rows vật lý cũ vẫn có thể tham chiếu (tránh xóa nhầm dữ liệu cũ).
/// </para>
/// </remarks>
public interface IReferenceCheckService
{
    /// <summary>
    /// Quét mọi nơi đang tham chiếu giá trị <paramref name="pkValue"/> của bảng danh mục
    /// <paramref name="catalogTableId"/>.
    /// </summary>
    /// <param name="catalogTableId">Table_Id (Sys_Table) của bảng danh mục đang muốn xóa.</param>
    /// <param name="pkValue">Giá trị PK của bản ghi sắp xóa.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// Danh sách nơi đang dùng (rỗng = an toàn để xóa). Mỗi phần tử gồm bảng.cột + số dòng khớp.
    /// </returns>
    Task<IReadOnlyList<ReferenceUsage>> CheckUsageAsync(
        int catalogTableId, object? pkValue, CancellationToken ct = default);
}

/// <summary>Một nơi đang tham chiếu bản ghi danh mục (kết quả soft-check).</summary>
/// <param name="Schema">SQL schema của bảng tham chiếu.</param>
/// <param name="Table">Tên bảng tham chiếu (Table_Code).</param>
/// <param name="Column">Cột tham chiếu (trùng tên PK hoặc hậu tố _PK).</param>
/// <param name="RowCount">Số dòng đang dùng giá trị này.</param>
/// <param name="IsLegacy">true = metadata Sys_Table/Sys_Column đã Is_Active=0 (dữ liệu cũ).</param>
public sealed record ReferenceUsage(
    string Schema,
    string Table,
    string Column,
    int    RowCount,
    bool   IsLegacy);
