// File    : ICacheVersion.cs
// Module  : Common
// Layer   : Application
// Purpose : Version-stamp cache theo tenant (ADR-014 / CC-4a). Mọi cache key "dùng chung" gắn
//           version này; bump version = vô hiệu TOÀN BỘ cache config của tenant trong 1 thao tác
//           (không cần liệt kê/đụng từng key). Phục vụ nút "Cưỡng chế làm mới cache".

namespace ICare247.Application.Interfaces;

/// <summary>Cấp + tăng số version cache dùng chung theo tenant (key đổi → mọi entry cũ vô hiệu).</summary>
public interface ICacheVersion
{
    /// <summary>Version hiện tại của tenant (mặc định 0 nếu chưa bump).</summary>
    int Get(int tenantId);

    /// <summary>Tăng version của tenant lên 1 → mọi cache key gắn version cũ trở nên không tồn tại.</summary>
    void Bump(int tenantId);
}
