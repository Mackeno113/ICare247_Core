// File    : AppState.cs
// Module  : Shared
// Layer   : Frontend (Shared)
// Purpose : Trạng thái phiên dùng chung toàn app — công ty đang chọn (multi-company) để mọi module +
//           DelegatingHandler (X-Active-CongTy) truy vấn theo đúng công ty. VFILTER-ACTIVE (ADR-030).

namespace ICare247.UI.Shared.State;

/// <summary>Một công ty trong company-switcher (Id = CongTy_Id bigint, gửi qua header X-Active-CongTy).</summary>
public sealed record CompanyOption(long Id, string? Code, string Name, bool IsDefault);

/// <summary>
/// Giữ trạng thái dùng chung giữa các module trong 1 phiên Blazor WASM (Scoped — 1 instance/phiên).
/// Module cần biết "đang làm việc với công ty nào" thì inject <see cref="AppState"/> + lắng nghe
/// <see cref="OnChange"/>. <see cref="ActiveCompanyId"/> null = "tất cả công ty được phân quyền"
/// (header bị bỏ → server dùng default @CongTyID_Active = 0).
/// </summary>
public sealed class AppState
{
    /// <summary>Công ty đang chọn (CongTy_Id). null = tất cả công ty.</summary>
    public long? ActiveCompanyId { get; private set; }

    /// <summary>Tên công ty đang chọn (null khi ActiveCompanyId null).</summary>
    public string? ActiveCompanyName { get; private set; }

    /// <summary>Danh sách công ty user được phép chọn (đổ từ /me/companies).</summary>
    public IReadOnlyList<CompanyOption> Companies { get; private set; } = [];

    /// <summary>Bắn khi trạng thái thay đổi để component re-render (StateHasChanged).</summary>
    public event Action? OnChange;

    /// <summary>Nạp danh sách công ty cho switcher. Sự kiện theo sau: <see cref="OnChange"/>.</summary>
    public void SetCompanies(IReadOnlyList<CompanyOption> companies)
    {
        Companies = companies ?? [];
        OnChange?.Invoke();
    }

    /// <summary>
    /// Đổi công ty hiện hành (null = tất cả). Tên suy từ <see cref="Companies"/>. Sự kiện theo sau:
    /// <see cref="OnChange"/> được bắn để mọi component lắng nghe vẽ lại + request kế tiếp gắn header mới.
    /// </summary>
    public void SetActiveCompany(long? companyId)
    {
        ActiveCompanyId = companyId;
        ActiveCompanyName = companyId is null
            ? null
            : Companies.FirstOrDefault(c => c.Id == companyId.Value)?.Name;
        OnChange?.Invoke();
    }
}
