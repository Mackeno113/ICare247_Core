// File    : GetMyNavigationQuery.cs
// Module  : Navigation
// Layer   : Application
// Purpose : Query lấy cây menu (đã lọc quyền) của người dùng hiện tại cho /me/navigation.

using MediatR;

namespace ICare247.Application.Features.Navigation.Queries.GetMyNavigation;

/// <summary>
/// Lấy menu user được thấy. Tenant ngầm định qua connection Data DB (factory tự resolve
/// theo ITenantContext) nên chỉ cần <paramref name="UserId"/>.
/// </summary>
/// <param name="UserId">Id người dùng (claim sub trong JWT).</param>
public sealed record GetMyNavigationQuery(long UserId) : IRequest<MeNavigationDto>;
