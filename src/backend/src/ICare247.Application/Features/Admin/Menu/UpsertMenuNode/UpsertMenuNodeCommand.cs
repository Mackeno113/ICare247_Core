// File    : UpsertMenuNodeCommand.cs
// Module  : Admin/Menu
// Layer   : Application
// Purpose : Thêm/sửa 1 node menu (HT_ChucNang) từ Menu Builder. Resolve NodeKind → các cột
//           Loai/DuongDan/DoiTuong/LoaiDoiTuong (whitelist), sinh Ma duy nhất khi thêm,
//           chống vòng lặp cha-con, rồi invalidate cache menu của tenant.

using ICare247.Application.Features.Admin.Menu;
using ICare247.Application.Interfaces;
using MediatR;

namespace ICare247.Application.Features.Admin.Menu.UpsertMenuNode;

/// <summary>Thêm (Id=null) hoặc sửa (Id có giá trị) 1 node menu.</summary>
/// <param name="Id">Null = thêm mới; có giá trị = sửa.</param>
/// <param name="NodeKind">Group | View | Form — quyết định route + đối tượng quyền.</param>
/// <param name="Ten">Tên hiển thị (bắt buộc).</param>
/// <param name="ParentId">Node cha (null = gốc).</param>
/// <param name="ObjectCode">ViewCode (View) / FormCode (Form). Null với Group.</param>
/// <param name="DuongDanOverride">Đường dẫn ghi đè (tùy chọn). Có giá trị → dùng thay route tự suy.</param>
/// <param name="Module">Mã phân hệ (tùy chọn).</param>
/// <param name="Icon">Icon (tùy chọn).</param>
/// <param name="ThuTu">Thứ tự trong cấp.</param>
/// <param name="KichHoat">Bật/tắt node.</param>
/// <param name="UserId">Người thao tác (CreatedBy/UpdatedBy).</param>
public sealed record UpsertMenuNodeCommand(
    long? Id, string NodeKind, string Ten, long? ParentId, string? ObjectCode,
    string? DuongDanOverride, string? Module, string? Icon, int ThuTu, bool KichHoat, long UserId) : IRequest<long>;

public sealed class UpsertMenuNodeCommandHandler : IRequestHandler<UpsertMenuNodeCommand, long>
{
    private readonly IMenuAdminRepository _repo;
    private readonly INavigationCache _navCache;
    private readonly ITenantContext _tenant;

    public UpsertMenuNodeCommandHandler(
        IMenuAdminRepository repo, INavigationCache navCache, ITenantContext tenant)
    {
        _repo = repo;
        _navCache = navCache;
        _tenant = tenant;
    }

    public async Task<long> Handle(UpsertMenuNodeCommand r, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(r.Ten))
            throw new ArgumentException("Tên node là bắt buộc.");
        if (!MenuNodeKind.IsValid(r.NodeKind))
            throw new ArgumentException($"NodeKind không hợp lệ: '{r.NodeKind}'.");

        var (loai, duongDan, doiTuong, loaiDoiTuong) = ResolveKind(r.NodeKind, r.ObjectCode);

        // Đường dẫn ghi đè: admin nhập tay khi route tự suy không đúng (vd node Nhóm trỏ trang tĩnh /m/...).
        // Có giá trị → thay route tự suy; rỗng → giữ route tự suy theo NodeKind.
        duongDan = NormalizeRouteOverride(r.DuongDanOverride) ?? duongDan;

        // Chống vòng lặp: cha mới không được là chính node hoặc nằm trong nhánh con của nó.
        if (r.Id is { } id && r.ParentId is { } pid)
        {
            if (pid == id || await _repo.IsDescendantAsync(id, pid, ct))
                throw new ArgumentException("Node cha không hợp lệ (tạo vòng lặp trong cây menu).");
        }

        long resultId;
        if (r.Id is null)
        {
            var ma = await GenerateUniqueMaAsync(r.NodeKind, r.ObjectCode, r.Ten, ct);
            var node = new MenuNodeWrite(ma, r.Ten.Trim(), r.ParentId, loai, r.Module,
                duongDan, r.Icon, r.ThuTu, r.KichHoat, doiTuong, loaiDoiTuong);
            resultId = await _repo.InsertAsync(node, r.UserId, ct);
        }
        else
        {
            // Ma giữ nguyên khi sửa (chỉ dùng ở Insert); truyền chuỗi rỗng.
            var node = new MenuNodeWrite("", r.Ten.Trim(), r.ParentId, loai, r.Module,
                duongDan, r.Icon, r.ThuTu, r.KichHoat, doiTuong, loaiDoiTuong);
            var ok = await _repo.UpdateAsync(r.Id.Value, node, r.UserId, ct);
            if (!ok) throw new InvalidOperationException($"Không tìm thấy node #{r.Id} để cập nhật.");
            resultId = r.Id.Value;
        }

        _navCache.InvalidateTenant(_tenant.TenantId);
        return resultId;
    }

    /// <summary>NodeKind → (Loai, DuongDan, DoiTuong, LoaiDoiTuong). Whitelist tuyệt đối, không nhận route tự do.</summary>
    private static (string Loai, string? DuongDan, string? DoiTuong, string? LoaiDoiTuong)
        ResolveKind(string kind, string? objectCode)
    {
        switch (kind)
        {
            case MenuNodeKind.Group:
                return ("Menu", null, null, null);
            case MenuNodeKind.View:
                Require(objectCode, "View");
                return ("ManHinh", $"/view/{objectCode}", objectCode, "View");
            case MenuNodeKind.Form:
                Require(objectCode, "Form");
                return ("ManHinh", $"/master/{objectCode}", objectCode, "Form");
            default:
                throw new ArgumentException($"NodeKind không hỗ trợ: '{kind}'.");
        }
    }

    private static void Require(string? code, string kind)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException($"Cần chọn đối tượng ({kind}) cho node loại {kind}.");
    }

    /// <summary>Chuẩn hóa route ghi đè: rỗng → null (giữ tự suy); ngược lại trim + bắt buộc bắt đầu bằng '/'.</summary>
    private static string? NormalizeRouteOverride(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        var route = raw.Trim();
        if (!route.StartsWith('/'))
            throw new ArgumentException("Đường dẫn phải bắt đầu bằng '/'.");
        return route;
    }

    /// <summary>Sinh Ma gợi nhớ + đảm bảo duy nhất (append -2,-3… khi trùng).</summary>
    private async Task<string> GenerateUniqueMaAsync(
        string kind, string? objectCode, string ten, CancellationToken ct)
    {
        var baseMa = kind switch
        {
            MenuNodeKind.View => $"view.{objectCode}",
            MenuNodeKind.Form => $"form.{objectCode}",
            _ => "group." + Slug(ten)
        };

        var candidate = baseMa;
        var i = 2;
        while (await _repo.MaExistsAsync(candidate, ct))
            candidate = $"{baseMa}-{i++}";
        return candidate;
    }

    /// <summary>Slug đơn giản cho Ma node nhóm: chữ/số → giữ, còn lại → '-', gộp '-' thừa.</summary>
    private static string Slug(string s)
    {
        var chars = s.Trim().ToLowerInvariant()
            .Select(c => char.IsLetterOrDigit(c) ? c : '-');
        var slug = new string(chars.ToArray());
        while (slug.Contains("--")) slug = slug.Replace("--", "-");
        slug = slug.Trim('-');
        return string.IsNullOrEmpty(slug) ? "node" : slug;
    }
}
