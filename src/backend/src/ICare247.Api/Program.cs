// File    : Program.cs
// Module  : Api
// Layer   : Api
// Purpose : Composition root — khởi tạo host, đăng ký DI, cấu hình middleware pipeline.

using System.Text;
using ICare247.Api;
using Microsoft.AspNetCore.DataProtection;
using ICare247.Api.Middleware;
using ICare247.Application;
using ICare247.Application.Interfaces;
using ICare247.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using Serilog;

// ── Bootstrap Serilog sớm để capture lỗi khởi động ──────────────────────────
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Khởi động ICare247 API...");

    var builder = WebApplication.CreateBuilder(args);

    // ── Local config file — nạp từ %APPDATA%\ICare247\Api\appsettings.local.json ──
    // Giống WPF ConfigStudio: file nằm ngoài repo, mỗi máy có file riêng.
    // Nếu chưa tồn tại → tự tạo template, in hướng dẫn ra console.
    // Thứ tự ưu tiên: appsettings.json < appsettings.Development.json
    //                 < appsettings.local.json < Environment Variables
    builder.AddLocalConfig();

    // ── DebugLogger — cấu hình từ section "DebugLog" trong local config ──────
    // Sau lệnh này DebugLogger.Enabled / WriteToFile được đọc từ appsettings.local.json
    DebugLogger.Configure(builder.Configuration);

    // ── Serilog ─────────────────────────────────────────────────────────────
    builder.Host.UseSerilog((ctx, services, cfg) => cfg
        .ReadFrom.Configuration(ctx.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithMachineName()
        .Enrich.WithProperty("Application", "ICare247.Api")
        .WriteTo.Console()
        .WriteTo.File(
            path: "logs/icare247-.log",
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 30));

    // ── Application + Infrastructure ────────────────────────────────────────
    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);

    // ── Tenant Context (scoped — set bởi TenantMiddleware) ─────────────────
    builder.Services.AddScoped<TenantContext>();
    builder.Services.AddScoped<ITenantContext>(sp => sp.GetRequiredService<TenantContext>());

    // ── JWT Auth ────────────────────────────────────────────────────────────
    var jwtSection = builder.Configuration.GetSection("Jwt");
    var secretKey = jwtSection["SecretKey"];

    // ── Lỗ hổng #3: fail-fast SecretKey ──────────────────────────────────────
    // Các giá trị placeholder bị commit vào git TUYỆT ĐỐI không được dùng để ký
    // token thật. Ngoài Development → dừng khởi động ngay nếu key rỗng/yếu/placeholder.
    var placeholderKeys = new[]
    {
        "PLACEHOLDER_CHANGE_VIA_LOCAL_CONFIG",
        "DEFAULT_DEV_KEY_CHANGE_IN_PRODUCTION_32CH",
        "DEV_ONLY_SECRET_KEY_DO_NOT_USE_IN_PRODUCTION_32CH"
    };
    var isWeakKey = string.IsNullOrWhiteSpace(secretKey)
        || secretKey!.Length < 32
        || placeholderKeys.Contains(secretKey, StringComparer.Ordinal);

    if (isWeakKey)
    {
        if (builder.Environment.IsDevelopment())
        {
            // Dev: cho chạy nhưng cảnh báo rõ — token chỉ có giá trị nội bộ.
            Log.Warning("Jwt:SecretKey đang là placeholder/yếu — CHỈ chấp nhận ở Development.");
            secretKey ??= "DEV_ONLY_SECRET_KEY_DO_NOT_USE_IN_PRODUCTION_32CH";
        }
        else
        {
            // Production/Staging: không cho ký token bằng key công khai.
            throw new InvalidOperationException(
                "Jwt:SecretKey chưa cấu hình an toàn (rỗng, < 32 ký tự, hoặc là giá trị " +
                "placeholder). Đặt key thật qua %APPDATA%\\ICare247\\Api\\appsettings.local.json " +
                "hoặc biến môi trường Jwt__SecretKey.");
        }
    }

    builder.Services
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(opts =>
        {
            opts.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSection["Issuer"],
                ValidAudience = jwtSection["Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(secretKey!)),
                ClockSkew = TimeSpan.FromMinutes(2)
            };

            // Development: cho phép anonymous (không bắt buộc token)
            if (builder.Environment.IsDevelopment())
            {
                opts.RequireHttpsMetadata = false;
            }
        });

    builder.Services.AddAuthorization();

    // ── DataProtection — keyring chia sẻ cho scale-out nhiều IIS (ADR-021) ──────
    // Khi tách site (app/web/từng module = IIS riêng) hoặc ≥2 instance API, MỌI node
    // phải dùng CHUNG keyring — nếu không, antiforgery token + cookie mã hóa sẽ vỡ khi
    // request rơi vào node khác. SetApplicationName cố định để mọi node nhận cùng key.
    //   • Multi-node: đặt "DataProtection:KeyPath" = đường UNC share chung
    //     (vd \\fileserver\icare247\dp-keys). Mọi node trỏ cùng path → cùng keyring.
    //   • Dev / 1 node: bỏ trống → ASP.NET tự lưu local (mặc định).
    //   • TODO(scale-out): chuyển keyring sang Redis (cần package
    //     Microsoft.AspNetCore.DataProtection.StackExchangeRedis) khi vận hành nhiều node.
    var dpBuilder = builder.Services.AddDataProtection()
        .SetApplicationName("ICare247.Api");
    var dpKeyPath = builder.Configuration["DataProtection:KeyPath"];
    if (!string.IsNullOrWhiteSpace(dpKeyPath))
    {
        dpBuilder.PersistKeysToFileSystem(new DirectoryInfo(dpKeyPath));
        Log.Information("DataProtection keyring lưu tại đường dẫn chia sẻ: {Path}", dpKeyPath);
    }

    // ── Controllers + OpenAPI ────────────────────────────────────────────────
    builder.Services.AddControllers();
    builder.Services.AddOpenApi();

    // ── Lỗ hổng #2: CORS whitelist (bỏ AllowAnyOrigin) ───────────────────────
    // AllowAnyOrigin không kết hợp được với credentials/cookie. Whitelist origin
    // từ config "Cors:AllowedOrigins". Dev: chấp nhận mọi origin loopback (localhost
    // bất kể cổng). Prod mà không khai báo → dừng khởi động, tránh mở toang.
    var allowedOrigins = builder.Configuration
        .GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

    builder.Services.AddCors(opts =>
        opts.AddDefaultPolicy(p =>
        {
            p.AllowAnyMethod().AllowAnyHeader().AllowCredentials();

            if (allowedOrigins.Length > 0)
            {
                p.WithOrigins(allowedOrigins);
            }
            else if (builder.Environment.IsDevelopment())
            {
                p.SetIsOriginAllowed(origin =>
                    Uri.TryCreate(origin, UriKind.Absolute, out var u) && u.IsLoopback);
            }
            else
            {
                throw new InvalidOperationException(
                    "Cors:AllowedOrigins rỗng ở môi trường ngoài Development. " +
                    "Khai báo danh sách origin frontend được phép.");
            }
        }));

    // ── Health Checks ────────────────────────────────────────────────────────
    builder.Services.AddHealthChecks();

    // ────────────────────────────────────────────────────────────────────────
    var app = builder.Build();

    // ── Kiểm tra kết nối DB + Redis ngay khi khởi động ───────────────────────
    // Không throw — chỉ log kết quả qua DebugLogger để debug nhanh
    await ConnectionChecker.CheckAllAsync(builder.Configuration);

    // ── Middleware pipeline (thứ tự quan trọng!) ─────────────────────────────

    // 1. Exception handling — phải đầu tiên để catch tất cả
    app.UseMiddleware<ExceptionHandlingMiddleware>();

    // 2. Correlation-Id — generate/extract trước khi log
    app.UseMiddleware<CorrelationMiddleware>();

    // 3. Request logging (Serilog) — sau correlation để log có CorrelationId
    app.UseSerilogRequestLogging(opts =>
    {
        opts.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
        {
            if (httpContext.Items.TryGetValue("CorrelationId", out var correlationId))
                diagnosticContext.Set("CorrelationId", correlationId);
        };
    });

    // 4. Dev-only: OpenAPI + Scalar UI
    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
        app.MapScalarApiReference();
    }

    // 5. Health check endpoint
    app.MapHealthChecks("/health");

    app.UseHttpsRedirection();
    app.UseCors();

    // 6. Tenant extraction — sau CORS, trước Auth
    app.UseMiddleware<TenantMiddleware>();

    // 7. Auth
    app.UseAuthentication();
    app.UseAuthorization();

    // 8. Controllers
    app.MapControllers();

    Log.Information("ICare247 API đã sẵn sàng.");
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "ICare247 API dừng do exception không xử lý được.");
    throw;
}
finally
{
    Log.CloseAndFlush();
}
