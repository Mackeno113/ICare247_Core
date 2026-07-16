// File    : SaveUserCommand.cs
// Module  : Admin/Users
// Layer   : Application
// Purpose : Command tạo mới / cập nhật thông tin người dùng từ màn Người dùng (admin).
//           Id null = tạo mới (bắt buộc MatKhau, băm PBKDF2); Id có = cập nhật (không đụng mật khẩu).

using FluentValidation;
using FluentValidation.Results;
using ICare247.Application.Interfaces;
using MediatR;

namespace ICare247.Application.Features.Admin.Users.SaveUser;

/// <param name="Id">Null = tạo mới; có giá trị = cập nhật.</param>
/// <param name="MatKhau">Chỉ dùng khi tạo mới; bỏ qua khi cập nhật (đổi qua ResetUserPassword).</param>
/// <param name="ActorId">Người thao tác (claim sub) — ghi CreatedBy/UpdatedBy.</param>
public sealed record SaveUserCommand(
    long? Id, string Ma, string TenDangNhap, string? MatKhau, string TrangThai,
    bool LaQuanTri, bool KichHoatMobile, DateTime? HetHanTaiKhoan, bool DoiMatKhauLanSau,
    long ActorId) : IRequest<long>;

public sealed class SaveUserCommandHandler : IRequestHandler<SaveUserCommand, long>
{
    // Enum hệ thống (spec 11 §6.1) — C# là nguồn sự thật, label hiển thị qua i18n shell.
    private static readonly string[] TrangThaiHopLe = ["HoatDong", "TamKhoa", "NgungHoatDong"];

    private readonly IUserAdminRepository _repo;
    private readonly IPasswordHasher _hasher;

    public SaveUserCommandHandler(IUserAdminRepository repo, IPasswordHasher hasher)
    {
        _repo = repo;
        _hasher = hasher;
    }

    /// <summary>
    /// Validate (bắt buộc, trùng mã/username, trạng thái hợp lệ) rồi tạo/cập nhật user.
    /// Sự kiện theo sau: FE reload lưới; user mới chưa có vai trò/công ty (gán ở tab tương ứng).
    /// </summary>
    public async Task<long> Handle(SaveUserCommand r, CancellationToken ct)
    {
        var errors = new List<ValidationFailure>();

        if (string.IsNullOrWhiteSpace(r.Ma))
            errors.Add(new ValidationFailure(nameof(r.Ma), "Mã người dùng không được để trống."));
        if (string.IsNullOrWhiteSpace(r.TenDangNhap))
            errors.Add(new ValidationFailure(nameof(r.TenDangNhap), "Tên đăng nhập không được để trống."));
        if (!TrangThaiHopLe.Contains(r.TrangThai))
            errors.Add(new ValidationFailure(nameof(r.TrangThai), $"Trạng thái '{r.TrangThai}' không hợp lệ."));
        if (r.Id is null && (r.MatKhau is null || r.MatKhau.Length < 6))
            errors.Add(new ValidationFailure(nameof(r.MatKhau), "Mật khẩu tối thiểu 6 ký tự."));
        if (errors.Count > 0) throw new ValidationException(errors);

        var (maTrung, tenTrung) = await _repo.CheckDuplicateAsync(r.Ma.Trim(), r.TenDangNhap.Trim(), r.Id, ct);
        if (maTrung)
            errors.Add(new ValidationFailure(nameof(r.Ma), $"Mã '{r.Ma}' đã tồn tại."));
        if (tenTrung)
            errors.Add(new ValidationFailure(nameof(r.TenDangNhap), $"Tên đăng nhập '{r.TenDangNhap}' đã tồn tại."));
        if (errors.Count > 0) throw new ValidationException(errors);

        if (r.Id is null)
        {
            var hash = _hasher.Hash(r.MatKhau!);
            return await _repo.CreateUserAsync(
                r.Ma.Trim(), r.TenDangNhap.Trim(), hash, r.TrangThai, r.LaQuanTri,
                r.KichHoatMobile, r.HetHanTaiKhoan, r.DoiMatKhauLanSau, r.ActorId, ct);
        }

        var ok = await _repo.UpdateUserAsync(
            r.Id.Value, r.Ma.Trim(), r.TenDangNhap.Trim(), r.TrangThai, r.LaQuanTri,
            r.KichHoatMobile, r.HetHanTaiKhoan, r.DoiMatKhauLanSau, r.ActorId, ct);
        if (!ok) throw new KeyNotFoundException($"Người dùng Id={r.Id} không tồn tại.");
        return r.Id.Value;
    }
}
