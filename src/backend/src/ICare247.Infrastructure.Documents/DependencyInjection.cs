// File    : DependencyInjection.cs
// Module  : DocTemplate
// Layer   : Infrastructure (Documents)
// Purpose : Đăng ký dịch vụ module xuất tài liệu (DevExpress) vào DI.
// Spec    : docs/spec/28_DOC_TEMPLATE_SPEC.md §7.

using ICare247.Application.Interfaces;
using ICare247.Infrastructure.Documents.Internal;
using Microsoft.Extensions.DependencyInjection;

namespace ICare247.Infrastructure.Documents;

/// <summary>Extension đăng ký module Documents. Gọi từ Program.cs: <c>builder.Services.AddDocuments();</c></summary>
public static class DependencyInjection
{
    /// <summary>
    /// Đăng ký engine + repo + proc runner + renderer. Sự kiện theo sau: <see cref="IDocTemplateRenderer"/>
    /// sẵn sàng inject vào controller. Yêu cầu <c>AddInfrastructure</c> đã đăng ký connection factory + tenant context.
    /// </summary>
    public static IServiceCollection AddDocuments(this IServiceCollection services)
    {
        services.AddSingleton<DocxRenderEngine>();          // thuần định dạng, không trạng thái
        services.AddScoped<DocTemplateRepository>();
        services.AddScoped<DocProcRunner>();
        services.AddScoped<IDocTemplateRenderer, DocTemplateRenderer>();
        return services;
    }
}
