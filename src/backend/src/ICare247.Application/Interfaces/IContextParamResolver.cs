// File    : IContextParamResolver.cs
// Module  : Context
// Layer   : Application
// Purpose : Resolve giá trị token ngữ cảnh được THAM CHIẾU trong một SQL admin tự viết — server-side,
//           an toàn (Claim bất biến; ActiveScope validate theo quyền). Xem spec 19 + ADR-030.

namespace ICare247.Application.Interfaces;

/// <summary>
/// Phân giải giá trị các token <c>Sys_Context_Param</c> được tham chiếu (theo tên, không '@').
/// Trả map <c>tên → giá trị đã ép kiểu</c> CHỈ cho token đăng ký + được tham chiếu; token không khai = bỏ.
/// </summary>
public interface IContextParamResolver
{
    /// <summary>
    /// Resolve các token trong <paramref name="referencedNames"/> (không '@', so khớp không phân biệt hoa/thường).
    /// ActiveScope: đọc header + chạy Validate_Sql (bind @NguoiDungID,@val); sai → Default_Value.
    /// </summary>
    Task<IReadOnlyDictionary<string, object?>> ResolveAsync(
        IEnumerable<string> referencedNames, CancellationToken ct = default);

    /// <summary>
    /// Trong <paramref name="referencedNames"/>, trả tập tên có <c>Source_Kind='Claim'</c> — token định danh
    /// bất biến (vd <c>NguoiDungID</c>). Dùng để caller khóa các tên này khỏi bị filter/cha/tham số khác
    /// (do admin cấu hình hoặc client gửi) ghi đè khi trùng tên — token định danh PHẢI luôn thắng, vì đây
    /// thường là ranh giới phân quyền (vd <c>fnt_XyzTheoQuyen(@NguoiDungID)</c>).
    /// </summary>
    Task<IReadOnlySet<string>> GetClaimLockedNamesAsync(
        IEnumerable<string> referencedNames, CancellationToken ct = default);
}
