// File    : ConfigSyncOptions.cs
// Module  : ConfigSync
// Layer   : Application
// Purpose : Tham số điều khiển một lần chạy đồng bộ config master → tenant (CFGSYNC-2).

namespace ICare247.Application.ConfigSync;

/// <summary>
/// Tùy chọn cho một lần đồng bộ config (spec 16). Một chiều master → Config DB tenant hiện tại.
/// </summary>
public sealed class ConfigSyncOptions
{
    /// <summary>
    /// <c>true</c> = chỉ tính toán diff/preview, KHÔNG ghi xuống tenant (spec §8 dry-run).
    /// </summary>
    public bool DryRun { get; init; }

    /// <summary>Người/luồng kích hoạt (super admin username hoặc "provisioning") — ghi vào log sync.</summary>
    public string? TriggeredBy { get; init; }
}
