// File    : DependencyInjection.cs
// Module  : Application
// Layer   : Application
// Purpose : Đăng ký tất cả services của Application layer vào DI container.

using System.Reflection;
using FluentValidation;
using ICare247.Application.Behaviors;
using ICare247.Application.Engines;
using ICare247.Application.Engines.EventActions;
using ICare247.Application.Interfaces;
using ICare247.Domain.Engine;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace ICare247.Application;

/// <summary>
/// Extension methods đăng ký Application layer vào IServiceCollection.
/// Gọi từ Program.cs: builder.Services.AddApplication()
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        // ── MediatR — tự động scan toàn bộ Queries/Commands/Handlers ─────────
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));

        // ── FluentValidation — tự động scan toàn bộ Validators ───────────────
        services.AddValidatorsFromAssembly(assembly);

        // ── ValidationBehavior — tự động validate request trước khi tới handler ──
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        // ── AST Engine — singleton vì stateless (trừ compiled cache) ───────────
        services.AddSingleton<FunctionRegistry>(sp =>
        {
            var registry = new FunctionRegistry();
            BuiltinFunctions.RegisterAll(registry);
            return registry;
        });
        services.AddSingleton<AstParser>();
        services.AddSingleton<AstCompiler>();
        services.AddSingleton<IAstEngine, AstEngine>();

        // ── Validation Engine — scoped vì phụ thuộc scoped repositories ────────
        services.AddScoped<IValidationEngine, ValidationEngine>();

        // ── Event action handlers (Strategy + registry, AUDIT-5) — EventEngine dispatch qua IEnumerable.
        //    Thêm action mới = thêm 1 handler + 1 dòng đăng ký ở đây, KHÔNG sửa EventEngine (OCP). ──
        services.AddScoped<IEventActionHandler, SetValueActionHandler>();
        services.AddScoped<IEventActionHandler, SetVisibleActionHandler>();
        services.AddScoped<IEventActionHandler, SetRequiredActionHandler>();
        services.AddScoped<IEventActionHandler, SetReadOnlyActionHandler>();
        services.AddScoped<IEventActionHandler, SetEnabledActionHandler>();
        services.AddScoped<IEventActionHandler, ClearValueActionHandler>();
        services.AddScoped<IEventActionHandler, ShowMessageActionHandler>();
        services.AddScoped<IEventActionHandler, ReloadOptionsActionHandler>();
        services.AddScoped<IEventActionHandler, TriggerValidationActionHandler>();

        // ── Event Engine — scoped vì phụ thuộc scoped repositories + action handlers ──
        services.AddScoped<IEventEngine, EventEngine>();

        // ── Metadata Engine — scoped vì phụ thuộc scoped repositories ──────────
        // Load FormMetadata + ResourceMap với L1+L2 cache — dùng cho RuntimeController.
        services.AddScoped<IMetadataEngine, MetadataEngine>();

        // ── ConfigCache facade (ADR-014) — đọc mọi config qua cache-aside L1+L2 ────
        // Bọc MetadataEngine + repo i18n/lookup. Handler/web CHỈ inject IConfigCache (CC-0d).
        services.AddScoped<IConfigCache, ConfigCache>();

        // ── Layout lưới per-user (cache-aside L1+L2, key-space riêng, write-through) ──
        services.AddScoped<IUserGridLayoutStore, Engines.UserGridLayoutStore>();

        return services;
    }
}
