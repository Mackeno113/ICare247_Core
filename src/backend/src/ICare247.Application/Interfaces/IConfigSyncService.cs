// File    : IConfigSyncService.cs
// Module  : ConfigSync
// Layer   : Application
// Purpose : Hợp đồng dịch vụ đồng bộ config master → Config DB tenant (F1 — spec 16).

using ICare247.Application.ConfigSync;

namespace ICare247.Application.Interfaces;

/// <summary>
/// Đồng bộ cấu hình (Sys_*/Ui_*) từ Config DB "vàng" master xuống Config DB tenant hiện tại.
/// Một chiều master → tenant (spec §8). UPSERT theo MÃ + re-link FK theo mã (spec §3),
/// đúng thứ tự phụ thuộc (spec §2). Tenant đã tùy biến (Is_Customized=1) thì giữ nguyên (spec §4);
/// master gỡ bản hệ thống thì tenant đặt Is_Active=0 (tombstone, spec §5).
/// </summary>
/// <remarks>
/// Tenant đích = Config DB của request hiện tại (IDbConnectionFactory tenant-aware).
/// Tiền đề: đã chạy <c>db/050_alter_config_sync_flags.sql</c> trên cả master lẫn tenant.
/// </remarks>
public interface IConfigSyncService
{
    /// <summary>
    /// Chạy đồng bộ. Toàn bộ ghi xuống tenant nằm trong MỘT transaction (idempotent, spec §8).
    /// Dry-run (<see cref="ConfigSyncOptions.DryRun"/>) chỉ tính diff, không ghi.
    /// </summary>
    /// <param name="options">Tùy chọn (dry-run, người kích hoạt).</param>
    /// <param name="ct">Token hủy.</param>
    /// <returns>Tổng hợp số dòng I/U/deactivate/skip theo từng bảng.</returns>
    Task<ConfigSyncResult> SyncAsync(ConfigSyncOptions options, CancellationToken ct = default);
}
