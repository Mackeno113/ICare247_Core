// File    : ErrorLogDtos.cs
// Module  : Admin/ErrorLogs
// Layer   : Application
// Purpose : DTO cho màn Nhật ký lỗi (admin): danh sách + chi tiết stack trace.

namespace ICare247.Application.Features.Admin.ErrorLogs;

/// <summary>Một dòng trên lưới Nhật ký lỗi.</summary>
/// <param name="Id">NK_LoiHeThong.Id.</param>
/// <param name="ThoiGian">Thời điểm xảy ra lỗi (UTC).</param>
/// <param name="MaLoi">8 ký tự đầu CorrelationId — đúng như hiển thị trên toast lỗi.</param>
/// <param name="Nguon">Loại exception (rút gọn tên class).</param>
/// <param name="ThongDiep">Message của exception.</param>
/// <param name="DuongDan">Request path.</param>
/// <param name="PhuongThuc">HTTP method.</param>
/// <param name="TenDangNhap">Người dùng đang thao tác lúc lỗi xảy ra (null nếu chưa đăng nhập).</param>
public sealed record ErrorLogListItemDto(
    long Id, DateTime ThoiGian, string MaLoi, string? Nguon, string? ThongDiep,
    string? DuongDan, string? PhuongThuc, string? TenDangNhap);

/// <summary>Chi tiết đầy đủ 1 lỗi — mở khi click dòng trên lưới (message + stack trace copy-able).</summary>
public sealed record ErrorLogDetailDto(
    long Id, DateTime ThoiGian, string CorrelationId, string? Nguon, string? ThongDiep, string? ChiTiet,
    string? DuongDan, string? PhuongThuc, long? NguoiDungId, string? TenDangNhap,
    string? DiaChiIp, string? ThietBi);
