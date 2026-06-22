// File    : IContextParamRepository.cs
// Module  : Context
// Layer   : Application
// Purpose : Đọc danh mục token ngữ cảnh Sys_Context_Param (Config DB) cho resolver.

using ICare247.Domain.Entities.Context;

namespace ICare247.Application.Interfaces;

/// <summary>Đọc registry <c>Sys_Context_Param</c> (token ngữ cảnh đang bật) từ Config DB.</summary>
public interface IContextParamRepository
{
    /// <summary>Danh sách token <c>Is_Active=1</c> của tenant hiện tại.</summary>
    Task<IReadOnlyList<ContextParam>> GetActiveAsync(CancellationToken ct = default);
}
