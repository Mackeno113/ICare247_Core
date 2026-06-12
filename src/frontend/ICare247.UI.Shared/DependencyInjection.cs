// File    : DependencyInjection.cs
// Module  : Shared
// Layer   : Frontend (Shared)
// Purpose : Điểm đăng ký DI tập trung cho các dịch vụ cross-cutting của frontend.
//           Host (ICare247_UI) chỉ cần gọi 1 dòng AddIcare247UiShared().

using ICare247.UI.Shared.Services.Auth;
using ICare247.UI.Shared.Services.I18n;
using ICare247.UI.Shared.State;
using Microsoft.Extensions.DependencyInjection;

namespace ICare247.UI.Shared;

/// <summary>
/// Tập hợp đăng ký DI cho tầng Shared. Mỗi module RCL về sau sẽ có
/// DependencyInjection riêng (AddIcare247UiOrganization()…) theo cùng pattern này.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Đăng ký các dịch vụ dùng chung (state, auth) vào container của host.
    /// Sự kiện theo sau: host có thể inject <see cref="AppState"/> và
    /// <see cref="IAuthService"/> ở mọi component.
    /// </summary>
    /// <param name="services">Service collection của host WASM.</param>
    /// <returns>Chính <paramref name="services"/> để nối chuỗi đăng ký.</returns>
    public static IServiceCollection AddIcare247UiShared(this IServiceCollection services)
    {
        services.AddScoped<AppState>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<LocalizationService>();
        return services;
    }
}
