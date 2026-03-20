// File    : DependencyInjection.cs
// Module  : Application
// Layer   : Application
// Purpose : Đăng ký tất cả services của Application layer vào DI container.

using System.Reflection;
using FluentValidation;
using ICare247.Application.Engines;
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

        // TODO(phase3): Đăng ký ValidationBehavior pipeline (MediatR pipeline behavior)

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

        // ── Event Engine — scoped vì phụ thuộc scoped repositories + ValidationEngine ──
        services.AddScoped<IEventEngine, EventEngine>();

        return services;
    }
}
