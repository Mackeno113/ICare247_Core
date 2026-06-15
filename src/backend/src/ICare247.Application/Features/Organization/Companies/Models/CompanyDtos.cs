// File    : CompanyDtos.cs
// Module  : Organization / Companies
// Layer   : Application
// Purpose : DTO + record cho cụm màn Công ty (TC_CongTy). Tree node cho lưới cây,
//           detail cho form 1:1, input cho lưu, option cho lookup, result cho lưu/xóa.

namespace ICare247.Application.Features.Organization.Companies.Models;

/// <summary>Một nút công ty trên lưới cây (DxTreeList) — phẳng, dựng cây qua Id/ChaId.</summary>
public sealed record CompanyTreeNodeDto(
    long Id, long? ChaId, string Ma, string Ten, string? TenVietTat,
    string? CapTen, string? MaSoThue, string TrangThai);

/// <summary>Chi tiết 1 công ty cho form 1:1 (gồm tên hiển thị của các lookup đang chọn).</summary>
public sealed record CompanyDetailDto(
    long Id, string Ma, string Ten, string? TenVietTat,
    long? CongTyChaId, long CapCongTyId, string? MaSoThue,
    string? DiaChi, long? PhuongXaId, string? DienThoai, string? Email, string? Website,
    string? NguoiDaiDien, string? GiamDoc, string? KeToanTruong,
    long? NganHangId, string? SoTaiKhoan, long? LogoId, string TrangThai,
    string? CapTen, string? PhuongXaTen, string? TinhTen, string? NganHangTen);

/// <summary>Giá trị có thể chỉnh từ form (không gồm khối audit/IsDeleted/Logo).</summary>
public sealed record CompanyInput(
    string Ma, string Ten, string? TenVietTat,
    long? CongTyChaId, long CapCongTyId, string? MaSoThue,
    string? DiaChi, long? PhuongXaId, string? DienThoai, string? Email, string? Website,
    string? NguoiDaiDien, string? GiamDoc, string? KeToanTruong,
    long? NganHangId, string? SoTaiKhoan, string TrangThai);

/// <summary>Option lookup chung (Id + nhãn + thông tin phụ tùy chọn, vd tên tỉnh của phường-xã).</summary>
public sealed record LookupOptionDto(long Id, string Text, string? Extra = null);

/// <summary>Bộ option tham chiếu cho form (tập nhỏ, nạp 1 lần): cấp công ty + ngân hàng.</summary>
public sealed record CompanyFormOptionsDto(
    IReadOnlyList<LookupOptionDto> CapCongTy,
    IReadOnlyList<LookupOptionDto> NganHang);

/// <summary>Kết quả lưu công ty: thành công + Id; hoặc danh sách lỗi (key i18n hoặc text).</summary>
public sealed record SaveCompanyResult(bool Success, long? Id, IReadOnlyList<string> Errors);

/// <summary>Kết quả xóa: thành công; hoặc bị chặn kèm lý do (key i18n).</summary>
public sealed record DeleteCompanyResult(bool Success, string? Reason);
