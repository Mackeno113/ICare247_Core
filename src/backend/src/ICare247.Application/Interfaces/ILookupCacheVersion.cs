// File    : ILookupCacheVersion.cs
// Module  : Common
// Layer   : Application
// Purpose : Version-stamp cache DỮ LIỆU lookup động theo (tenant, bảng nguồn). Cache key của
//           lookup gắn version này; khi bảng nguồn đổi (lưu danh mục) → Bump → mọi cache đọc
//           bảng đó vô hiệu (không cần liệt kê từng key theo context param). Tách khỏi
//           ICacheVersion (per-tenant, cho cache config) để lưu danh mục KHÔNG flush cache form.

namespace ICare247.Application.Interfaces;

/// <summary>Version-stamp cache dữ liệu lookup theo (tenant, tên bảng nguồn). In-memory.</summary>
public interface ILookupCacheVersion
{
    /// <summary>Version hiện tại của (tenant, bảng). Mặc định 0 nếu chưa bump.</summary>
    int Get(int tenantId, string sourceTable);

    /// <summary>Tăng version của (tenant, bảng) → mọi cache lookup đọc bảng này vô hiệu.</summary>
    void Bump(int tenantId, string sourceTable);
}
