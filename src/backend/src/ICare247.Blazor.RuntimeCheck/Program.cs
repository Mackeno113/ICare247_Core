// File    : Program.cs
// Module  : RuntimeCheck
// Purpose : Blazor WASM bootstrap — đăng ký services, cấu hình HttpClient.

using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using ICare247.Blazor.RuntimeCheck;
using ICare247.Blazor.RuntimeCheck.Models;
using ICare247.Blazor.RuntimeCheck.Services;

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
builder.Services.AddScoped(sp =>
{
    var settings = sp.GetRequiredService<ApiSettings>();
    var client = new HttpClient
    {
        BaseAddress = new Uri(settings.BaseUrl)
    };
    // Header X-Tenant-Id bắt buộc cho TenantMiddleware phía backend
    client.DefaultRequestHeaders.Add("X-Tenant-Id", settings.TenantId.ToString());
    return client;
});

// ── Application services ───────────────────────────────────────────────────
builder.Services.AddScoped<FormApiService>();
builder.Services.AddScoped<RuntimeApiService>();
builder.Services.AddScoped<LookupApiService>();
builder.Services.AddScoped<ILookupQueryService, LookupQueryService>();

await builder.Build().RunAsync();
