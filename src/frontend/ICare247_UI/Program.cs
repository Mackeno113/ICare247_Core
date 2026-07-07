// File    : Program.cs
// Module  : ICare247_UI
// Purpose : Blazor WASM bootstrap — đăng ký services, cấu hình HttpClient.

using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using ICare247_UI;
using ICare247_UI.Models;
using ICare247_UI.Services;
using ICare247.UI.Shared;
using ICare247.UI.Shared.Services.Auth;
using ICare247.UI.Shared.State;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// ── Logging — Blazor WASM ghi ra browser console (F12 → Console tab) ──────
// Development: Debug level để thấy tất cả request/response log
builder.Logging.SetMinimumLevel(
    builder.HostEnvironment.IsDevelopment()
        ? Microsoft.Extensions.Logging.LogLevel.Debug
        : Microsoft.Extensions.Logging.LogLevel.Warning);

// ── Đọc cấu hình ApiSettings từ wwwroot/appsettings.json ──────────────────
var apiSettings = new ApiSettings();
builder.Configuration.GetSection("ApiSettings").Bind(apiSettings);
builder.Services.AddSingleton(apiSettings);

// ── HttpClient — gọi backend API ──────────────────────────────────────────
// BaseAddress = URL API backend (không phải URL của Blazor app)

// SEC2-2: TokenRefresher dùng HttpClient RIÊNG (bare, KHÔNG gắn RefreshTokenHandler) → tránh đệ quy
// khi /auth/refresh được gọi từ trong handler 401.
builder.Services.AddScoped(sp =>
{
    var settings = sp.GetRequiredService<ApiSettings>();
    var bare = new HttpClient { BaseAddress = new Uri(settings.BaseUrl) };
    bare.DefaultRequestHeaders.Add("X-Tenant-Id", settings.TenantId.ToString());
    return new TokenRefresher(bare,
        sp.GetRequiredService<TokenStore>(),
        sp.GetRequiredService<JwtAuthenticationStateProvider>());
});

// HttpClient chính: chuỗi handler ActiveScopeHandler → RefreshTokenHandler → HttpClientHandler.
//   • RefreshTokenHandler: tự đính Bearer từ TokenStore + 401→refresh→retry.
//   • ActiveScopeHandler (NGOÀI CÙNG): đính X-Active-CongTy từ AppState; đặt ngoài để header có sẵn cả
//     khi RefreshTokenHandler clone request retry sau refresh.
builder.Services.AddScoped(sp =>
{
    var settings = sp.GetRequiredService<ApiSettings>();
    var refreshHandler = new RefreshTokenHandler(
        sp.GetRequiredService<TokenStore>(),
        sp.GetRequiredService<TokenRefresher>())
    {
        InnerHandler = new HttpClientHandler()
    };
    var activeScopeHandler = new ActiveScopeHandler(sp.GetRequiredService<AppState>())
    {
        InnerHandler = refreshHandler
    };
    var client = new HttpClient(activeScopeHandler)
    {
        BaseAddress = new Uri(settings.BaseUrl)
    };
    // Header X-Tenant-Id bắt buộc cho TenantMiddleware phía backend
    client.DefaultRequestHeaders.Add("X-Tenant-Id", settings.TenantId.ToString());
    return client;
});

// ── DevExpress Blazor ──────────────────────────────────────────────────────
// NOTE: BootstrapVersion obsolete từ v25 — AddDevExpressBlazor() không cần options
builder.Services.AddDevExpressBlazor();

// ── Dịch vụ Shared (cross-cutting: AppState, Auth) ─────────────────────────
builder.Services.AddIcare247UiShared();

// ── Application services ───────────────────────────────────────────────────
builder.Services.AddScoped<FormApiService>();
builder.Services.AddScoped<RuntimeApiService>();
builder.Services.AddScoped<LookupApiService>();
builder.Services.AddScoped<ILookupQueryService, LookupQueryService>();
builder.Services.AddScoped<MasterDataApiService>();
builder.Services.AddScoped<ViewApiService>();
builder.Services.AddScoped<ImportApiService>();
builder.Services.AddScoped<NavigationApiService>();
builder.Services.AddScoped<MeCompanyApiService>();
builder.Services.AddScoped<PermissionState>();
builder.Services.AddScoped<AdminPermissionApiService>();
builder.Services.AddScoped<MenuAdminApiService>();
builder.Services.AddScoped<ConfigSyncApiService>();
builder.Services.AddScoped<CacheAdminApiService>();
builder.Services.AddScoped<GridLayoutService>();
builder.Services.AddScoped<AttachmentApiService>();

await builder.Build().RunAsync();
