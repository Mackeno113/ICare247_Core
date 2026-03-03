// File    : Program.cs
// Module  : Api
// Layer   : Api
// Purpose : Composition root — khởi tạo host, đăng ký DI, cấu hình middleware pipeline.

using ICare247.Application;
using ICare247.Infrastructure;
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

    // ── Serilog ─────────────────────────────────────────────────────────────
    builder.Host.UseSerilog((ctx, services, cfg) => cfg
        .ReadFrom.Configuration(ctx.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithMachineName()
        .WriteTo.Console()
        .WriteTo.File(
            path: "logs/icare247-.log",
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 30));

    // ── Application + Infrastructure ────────────────────────────────────────
    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);

    // ── Auth ────────────────────────────────────────────────────────────────
    builder.Services.AddAuthentication().AddJwtBearer();
    builder.Services.AddAuthorization();

    // ── Controllers + OpenAPI ────────────────────────────────────────────────
    builder.Services.AddControllers();
    builder.Services.AddOpenApi();

    // ── CORS ─────────────────────────────────────────────────────────────────
    builder.Services.AddCors(opts =>
        opts.AddDefaultPolicy(p => p
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader()));

    // ────────────────────────────────────────────────────────────────────────
    var app = builder.Build();

    // ── Middleware pipeline ──────────────────────────────────────────────────
    app.UseSerilogRequestLogging();

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
        // Scalar UI tại /scalar/v1
        app.MapScalarApiReference();
    }

    app.UseHttpsRedirection();
    app.UseCors();

    // TODO(phase2): Thêm middleware Tenant extraction (X-Tenant-Id header)
    // TODO(phase2): Thêm middleware Correlation-Id

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();

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
