// File    : Program.cs
// Module  : Api
// Layer   : Api
// Purpose : Composition root — khởi tạo host, đăng ký DI, cấu hình middleware pipeline.

using System.Text;
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
    var secretKey = jwtSection["SecretKey"] ?? "DEFAULT_DEV_KEY_CHANGE_IN_PRODUCTION_32CH";

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
                    Encoding.UTF8.GetBytes(secretKey)),
                ClockSkew = TimeSpan.FromMinutes(2)
            };

            // Development: cho phép anonymous (không bắt buộc token)
            if (builder.Environment.IsDevelopment())
            {
                opts.RequireHttpsMetadata = false;
            }
        });

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

    // ── Health Checks ────────────────────────────────────────────────────────
    builder.Services.AddHealthChecks();

    // ────────────────────────────────────────────────────────────────────────
    var app = builder.Build();

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
