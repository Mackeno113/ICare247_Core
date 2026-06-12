// File    : AppState.cs
// Module  : Shared
// Layer   : Frontend (Shared)
// Purpose : Trạng thái phiên dùng chung toàn app — công ty hiện hành (multi-tenant /
//           multi-company) cho mọi module truy vấn theo đúng công ty đang chọn.

namespace ICare247.UI.Shared.State;

/// <summary>
/// Giữ trạng thái dùng chung giữa các module trong 1 phiên Blazor WASM.
/// Đăng ký Scoped — mỗi phiên người dùng có 1 instance. Module nào cần biết
/// "đang làm việc với công ty nào" thì inject <see cref="AppState"/> và lắng nghe
/// <see cref="OnChange"/>.
/// </summary>
public sealed class AppState
{
    /// <summary>Công ty đang được chọn trên thanh switcher (null = chưa chọn).</summary>
    public Guid? CurrentCompanyId { get; private set; }

    /// <summary>Bắn khi trạng thái thay đổi để component re-render (StateHasChanged).</summary>
    public event Action? OnChange;

    /// <summary>
    /// Đổi công ty hiện hành. Sự kiện theo sau: <see cref="OnChange"/> được bắn để
    /// mọi component đang lắng nghe vẽ lại theo công ty mới.
    /// </summary>
    /// <param name="companyId">Id công ty được chọn.</param>
    public void SetCompany(Guid companyId)
    {
        CurrentCompanyId = companyId;
        OnChange?.Invoke();
    }
}
