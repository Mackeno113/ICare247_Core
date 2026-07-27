// File    : AdminErrorLogModels.cs
// Module  : ICare247_UI
// Purpose : ViewModel màn Nhật ký lỗi (admin): dòng lưới + chi tiết stack trace.

namespace ICare247_UI.Models;

/// <summary>1 dòng trên lưới Nhật ký lỗi (GET /api/v1/admin/error-logs).</summary>
public sealed class ErrorLogListVm
{
    public long Id { get; set; }
    public DateTime ThoiGian { get; set; }
    public string MaLoi { get; set; } = "";
    public string? Nguon { get; set; }
    public string? ThongDiep { get; set; }
    public string? DuongDan { get; set; }
    public string? PhuongThuc { get; set; }
    public string? TenDangNhap { get; set; }
}

/// <summary>Chi tiết 1 lỗi (GET /api/v1/admin/error-logs/{id}) — message + stack trace đầy đủ.</summary>
public sealed class ErrorLogDetailVm
{
    public long Id { get; set; }
    public DateTime ThoiGian { get; set; }
    public string CorrelationId { get; set; } = "";
    public string? Nguon { get; set; }
    public string? ThongDiep { get; set; }
    public string? ChiTiet { get; set; }
    public string? DuongDan { get; set; }
    public string? PhuongThuc { get; set; }
    public long? NguoiDungId { get; set; }
    public string? TenDangNhap { get; set; }
    public string? DiaChiIp { get; set; }
    public string? ThietBi { get; set; }
}
