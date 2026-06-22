// File    : ContextParam.cs
// Module  : Context
// Layer   : Domain
// Purpose : Một token ngữ cảnh đăng ký ở Sys_Context_Param — engine bind server-side cho SQL
//           admin tự viết (Lookup_Sql bộ lọc, Source SP/SQL). Xem spec 19 + ADR-030.

namespace ICare247.Domain.Entities.Context;

/// <summary>
/// Token ngữ cảnh (<c>Sys_Context_Param</c>) — giá trị resolve SERVER-SIDE theo <see cref="SourceKind"/>,
/// KHÔNG lấy thô từ client. Whitelist bind = (registry) ∪ (param khai trong Ui_View_Filter).
/// </summary>
public sealed class ContextParam
{
    public int ParamId { get; init; }

    /// <summary>Tên token KHÔNG có '@' (vd <c>CongTyID_Active</c>). Client gửi value không qua đây.</summary>
    public string ParamName { get; init; } = string.Empty;

    /// <summary>bigint | int | string | decimal | date | bool — cách ép kiểu giá trị bind.</summary>
    public string SqlType { get; init; } = "string";

    /// <summary>Claim | Header | ActiveScope — nguồn lấy giá trị.</summary>
    public string SourceKind { get; init; } = "Claim";

    /// <summary>Tên claim (vd <c>sub</c>) hoặc tên header (vd <c>X-Active-CongTy</c>) để đọc.</summary>
    public string SourceKey { get; init; } = string.Empty;

    /// <summary>Chỉ <c>ActiveScope</c> — SQL trả 1/0 (bind <c>@NguoiDungID</c>, <c>@val</c>); rỗng → ép Default.</summary>
    public string? ValidateSql { get; init; }

    /// <summary>Giá trị mặc định khi rỗng / không hợp lệ (vd <c>0</c> = bỏ thu hẹp).</summary>
    public string? DefaultValue { get; init; }

    public string? Description { get; init; }

    /// <summary>Token lõi nền tảng (đồng bộ master→tenant).</summary>
    public bool IsSystem { get; init; }
}
