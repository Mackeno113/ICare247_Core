// File    : CompanyModels.cs
// Module  : ICare247.UI.Organization
// Layer   : Frontend (RCL)
// Purpose : DTO khớp API /api/v1/organization/companies + model chỉnh sửa (mutable) cho form.

namespace ICare247.UI.Organization.Services;

/// <summary>Nút công ty trên lưới cây (phẳng — TreeList dựng cây qua Id/ChaId).</summary>
public sealed class CompanyTreeNode
{
    public long Id { get; set; }
    public long? ChaId { get; set; }
    public string Ma { get; set; } = "";
    public string Ten { get; set; } = "";
    public string? TenVietTat { get; set; }
    public string? CapTen { get; set; }
    public string? MaSoThue { get; set; }
    public string TrangThai { get; set; } = "";
}

/// <summary>Chi tiết 1 công ty (form Sửa).</summary>
public sealed class CompanyDetail
{
    public long Id { get; set; }
    public string Ma { get; set; } = "";
    public string Ten { get; set; } = "";
    public string? TenVietTat { get; set; }
    public long? CongTyChaId { get; set; }
    public long CapCongTyId { get; set; }
    public string? MaSoThue { get; set; }
    public string? DiaChi { get; set; }
    public long? PhuongXaId { get; set; }
    public string? DienThoai { get; set; }
    public string? Email { get; set; }
    public string? Website { get; set; }
    public string? NguoiDaiDien { get; set; }
    public string? GiamDoc { get; set; }
    public string? KeToanTruong { get; set; }
    public long? NganHangId { get; set; }
    public string? SoTaiKhoan { get; set; }
    public long? LogoId { get; set; }
    public string TrangThai { get; set; } = "HoatDong";
    public string? CapTen { get; set; }
    public string? PhuongXaTen { get; set; }
    public string? TinhTen { get; set; }
    public string? NganHangTen { get; set; }
}

/// <summary>Payload gửi lên khi lưu (khớp record CompanyInput của backend).</summary>
public sealed class CompanyInput
{
    public string Ma { get; set; } = "";
    public string Ten { get; set; } = "";
    public string? TenVietTat { get; set; }
    public long? CongTyChaId { get; set; }
    public long CapCongTyId { get; set; }
    public string? MaSoThue { get; set; }
    public string? DiaChi { get; set; }
    public long? PhuongXaId { get; set; }
    public string? DienThoai { get; set; }
    public string? Email { get; set; }
    public string? Website { get; set; }
    public string? NguoiDaiDien { get; set; }
    public string? GiamDoc { get; set; }
    public string? KeToanTruong { get; set; }
    public long? NganHangId { get; set; }
    public string? SoTaiKhoan { get; set; }
    public string TrangThai { get; set; } = "HoatDong";
}

/// <summary>Option lookup chung (Extra = thông tin phụ, vd tên tỉnh của phường-xã).</summary>
public sealed class LookupOption
{
    public long Id { get; set; }
    public string Text { get; set; } = "";
    public string? Extra { get; set; }
}

/// <summary>Bộ option tham chiếu cho form.</summary>
public sealed class CompanyFormOptions
{
    public List<LookupOption> CapCongTy { get; set; } = [];
    public List<LookupOption> NganHang { get; set; } = [];
}

/// <summary>Kết quả lưu (đọc cả khi 200 và 422).</summary>
public sealed class SaveCompanyResult
{
    public bool Success { get; set; }
    public long? Id { get; set; }
    public List<string> Errors { get; set; } = [];
}

/// <summary>Kết quả xóa (đọc cả khi 200 và 409).</summary>
public sealed class DeleteCompanyResult
{
    public bool Success { get; set; }
    public string? Reason { get; set; }
}
