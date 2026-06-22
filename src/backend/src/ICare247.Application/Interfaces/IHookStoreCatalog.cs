// File    : IHookStoreCatalog.cs
// Module  : MasterData
// Layer   : Application
// Purpose : Tra "màn có hook store hay không" (spc_Grid_/sp_AfterSave_Grid_) QUA CACHE —
//           tránh query OBJECT_ID vào Data DB mỗi lần lưu (ADR-029). Cache-aside L1/L2,
//           gắn version-stamp tenant (flush cache = vô hiệu). Nạp sẵn lúc mở list.

namespace ICare247.Application.Interfaces;

/// <summary>
/// Catalog (có cache) cho biết một bảng đích đã có hook store chưa.
/// Save path đọc cache → KHÔNG truy vấn DB khi lưu (chỉ cold-miss mới query 1 lần, gộp 2 store).
/// </summary>
public interface IHookStoreCatalog
{
    /// <summary>
    /// Trả cờ tồn tại 2 store của bảng <paramref name="tableName"/> (cache-aside).
    /// Tên bảng không hợp lệ / rỗng → <see cref="HookStoreFlags.None"/>.
    /// </summary>
    Task<HookStoreFlags> GetAsync(string tableName, int tenantId, CancellationToken ct = default);
}

/// <summary>Cờ tồn tại hook store của 1 bảng (cache được).</summary>
public sealed class HookStoreFlags
{
    /// <summary>Có <c>spc_Grid_&lt;Table&gt;</c> (validate trước ghi).</summary>
    public bool HasValidate { get; init; }
    /// <summary>Có <c>sp_AfterSave_Grid_&lt;Table&gt;</c> (hậu xử lý sau ghi).</summary>
    public bool HasAfterSave { get; init; }

    /// <summary>Không có store nào (bảng chưa bật hook / tên không hợp lệ).</summary>
    public static readonly HookStoreFlags None = new();
}
