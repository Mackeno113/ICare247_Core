// File    : LocalizedComponentBase.cs
// Module  : Shared
// Layer   : Frontend (Shared)
// Purpose : Lớp cơ sở cho component có chữ đa ngôn ngữ — tự vẽ lại khi đổi ngôn ngữ.
//           Component kế thừa rồi dùng Loc.L("key", "base vi") trong markup.

using ICare247.UI.Shared.Services.I18n;
using Microsoft.AspNetCore.Components;

namespace ICare247.UI.Shared.Components;

/// <summary>
/// Base component đăng ký lắng nghe đổi ngôn ngữ và gọi StateHasChanged.
/// </summary>
public abstract class LocalizedComponentBase : ComponentBase, IDisposable
{
    /// <summary>Dịch vụ ngôn ngữ — dùng trong markup: <c>@Loc.L("key", "base")</c>.</summary>
    [Inject] protected LocalizationService Loc { get; set; } = default!;

    /// <summary>Khởi tạo: lắng nghe sự kiện đổi ngôn ngữ.</summary>
    protected override void OnInitialized() => Loc.OnChanged += OnLanguageChanged;

    /// <summary>Vẽ lại component khi ngôn ngữ thay đổi.</summary>
    private void OnLanguageChanged() => InvokeAsync(StateHasChanged);

    /// <summary>Hủy đăng ký khi component bị loại bỏ.</summary>
    public virtual void Dispose() => Loc.OnChanged -= OnLanguageChanged;
}
