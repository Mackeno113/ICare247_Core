// File    : DependencyInjection.cs
// Module  : Application
// Layer   : Application
// Purpose : Đăng ký tất cả services của Application layer vào DI container.

using System.Reflection;
using FluentValidation;
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

        // TODO(phase2): Đăng ký ValidationBehavior pipeline (MediatR pipeline behavior)

        return services;
    }
}
