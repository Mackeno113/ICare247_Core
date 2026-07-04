// File    : ConfigSyncModels.cs
// Module  : ICare247_UI (host)
// Layer   : Frontend (UI)
// Purpose : Model màn Đồng bộ cấu hình — khớp ConfigSyncResult/ConfigSyncTableResult backend (CFGSYNC-3).

namespace ICare247_UI.Models;

/// <summary>Kết quả đồng bộ cho 1 bảng config (khớp ConfigSyncTableResult backend).</summary>
public sealed class ConfigSyncTableVm
{
    public string TableName { get; set; } = "";
    public int Inserted { get; set; }
    public int Updated { get; set; }
    public int Deactivated { get; set; }
    public int Skipped { get; set; }
}

/// <summary>Tổng hợp kết quả 1 lần đồng bộ (khớp ConfigSyncResult backend). Tổng tính tại FE từ Tables.</summary>
public sealed class ConfigSyncResultVm
{
    public bool DryRun { get; set; }
    public string Status { get; set; } = "";
    public string? ErrorMessage { get; set; }
    public List<ConfigSyncTableVm> Tables { get; set; } = new();

    /// <summary>Cảnh báo cấu hình (advisory) — vd cascade sai. Khớp ConfigSyncResult.Warnings backend.</summary>
    public List<string> Warnings { get; set; } = new();

    public int TotalInserted => Tables.Sum(t => t.Inserted);
    public int TotalUpdated => Tables.Sum(t => t.Updated);
    public int TotalDeactivated => Tables.Sum(t => t.Deactivated);
    public int TotalSkipped => Tables.Sum(t => t.Skipped);
}
