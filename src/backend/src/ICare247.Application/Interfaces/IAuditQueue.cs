// File    : IAuditQueue.cs
// Module  : Audit
// Layer   : Application
// Purpose : Hàng đợi in-memory (bounded) cho audit — tách producer (request) khỏi consumer (nền).
//           Đầy thì DROP (không chặn request). Định nghĩa ở Application để Api dùng mà không
//           phải tham chiếu trực tiếp Infrastructure.

using System.Threading.Channels;

namespace ICare247.Application.Interfaces;

/// <summary>
/// Hàng đợi nhật ký phi-chặn. Producer gọi <see cref="TryWrite"/> (O(1), không I/O); consumer
/// nền đọc qua <see cref="Reader"/>. Khi đầy: bỏ bản ghi mới + tăng <see cref="DroppedCount"/>.
/// </summary>
public interface IAuditQueue
{
    /// <summary>Đẩy 1 sự kiện. Trả false nếu hàng đợi đầy (đã bị drop). KHÔNG bao giờ chặn.</summary>
    bool TryWrite(AuditEvent e);

    /// <summary>Đầu đọc cho tiến trình nền.</summary>
    ChannelReader<AuditEvent> Reader { get; }

    /// <summary>Tổng số bản ghi bị bỏ do hàng đợi đầy (để cảnh báo/đo tải).</summary>
    long DroppedCount { get; }
}
