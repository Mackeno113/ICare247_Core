// File    : DependencyInjection.cs
// Module  : ICare247.UI.Organization
// Layer   : Frontend (RCL)
// Purpose : Đăng ký DI cho module Tổ chức. Host gọi 1 dòng AddIcare247UiOrganization().

using ICare247.UI.Organization.Services;
using Microsoft.Extensions.DependencyInjection;

namespace ICare247.UI.Organization;

/// <summary>Đăng ký dịch vụ module Tổ chức (Organization) vào container host.</summary>
public static class DependencyInjection
{
    /// <summary>Thêm các *ApiService của module Tổ chức. Trả về services để nối chuỗi.</summary>
    public static IServiceCollection AddIcare247UiOrganization(this IServiceCollection services)
    {
        services.AddScoped<CompanyApiService>();
        return services;
    }
}
