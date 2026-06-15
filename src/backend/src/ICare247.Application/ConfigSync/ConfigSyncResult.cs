// File    : ConfigSyncResult.cs
// Module  : ConfigSync
// Layer   : Application
// Purpose : Kết quả một lần đồng bộ config master → tenant — tổng hợp + chi tiết theo bảng.

namespace ICare247.Application.ConfigSync;

/// <summary>
/// Kết quả tổng hợp một lần đồng bộ config (spec 16). Dùng cho cả dry-run (preview) lẫn áp thật.
/// </summary>
public sealed class ConfigSyncResult
{
    /// <summary>Có phải chạy preview (không ghi) hay không.</summary>
    public bool DryRun { get; init; }

    /// <summary>Thời điểm bắt đầu (UTC theo đồng hồ server).</summary>
    public DateTime StartedAt { get; init; }

    /// <summary>Thời điểm kết thúc.</summary>
    public DateTime FinishedAt { get; set; }

    /// <summary>"Success" | "Failed". (Dry-run thành công vẫn là Success.)</summary>
    public string Status { get; set; } = "Success";

    /// <summary>Thông điệp lỗi nếu <see cref="Status"/> = "Failed".</summary>
    public string? ErrorMessage { get; set; }

    /// <summary>Kết quả từng bảng, theo đúng thứ tự đồng bộ (spec §2).</summary>
    public List<ConfigSyncTableResult> Tables { get; } = [];

    /// <summary>Tổng số dòng INSERT trên mọi bảng.</summary>
    public int TotalInserted => Tables.Sum(t => t.Inserted);

    /// <summary>Tổng số dòng UPDATE.</summary>
    public int TotalUpdated => Tables.Sum(t => t.Updated);

    /// <summary>Tổng số dòng tombstone (Is_Active = 0).</summary>
    public int TotalDeactivated => Tables.Sum(t => t.Deactivated);

    /// <summary>Tổng số dòng bỏ qua (Is_Customized = 1 hoặc thiếu khóa).</summary>
    public int TotalSkipped => Tables.Sum(t => t.Skipped);
}

/// <summary>Kết quả đồng bộ cho một bảng config.</summary>
public sealed class ConfigSyncTableResult
{
    /// <summary>Tên bảng (vd "Ui_Field").</summary>
    public string TableName { get; init; } = string.Empty;

    /// <summary>Số dòng thêm mới ở tenant.</summary>
    public int Inserted { get; set; }

    /// <summary>Số dòng cập nhật từ master.</summary>
    public int Updated { get; set; }

    /// <summary>Số dòng tenant bị ngừng (master đã gỡ) — đặt Is_Active = 0.</summary>
    public int Deactivated { get; set; }

    /// <summary>Số dòng bỏ qua: tenant đã tùy biến (Is_Customized=1) hoặc master thiếu khóa tự nhiên.</summary>
    public int Skipped { get; set; }
}
