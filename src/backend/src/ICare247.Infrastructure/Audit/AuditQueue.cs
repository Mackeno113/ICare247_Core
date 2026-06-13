// File    : AuditQueue.cs
// Module  : Audit
// Layer   : Infrastructure
// Purpose : Cài đặt IAuditQueue bằng System.Threading.Channels (bounded, DROP khi đầy).

using System.Threading.Channels;
using ICare247.Application.Interfaces;

namespace ICare247.Infrastructure.Audit;

/// <summary>
/// Hàng đợi audit in-memory bounded. <c>FullMode = DropWrite</c> → khi đầy, <see cref="TryWrite"/>
/// trả false ngay (không chặn request), đếm vào <see cref="DroppedCount"/>. Singleton.
/// </summary>
public sealed class AuditQueue : IAuditQueue
{
    private readonly Channel<AuditEvent> _channel;
    private long _dropped;

    /// <param name="capacity">Sức chứa tối đa trước khi drop (mặc định 10.000).</param>
    public AuditQueue(int capacity = 10_000)
    {
        _channel = Channel.CreateBounded<AuditEvent>(new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.DropWrite,
            SingleReader = true,
            SingleWriter = false
        });
    }

    /// <inheritdoc />
    public ChannelReader<AuditEvent> Reader => _channel.Reader;

    /// <inheritdoc />
    public long DroppedCount => Interlocked.Read(ref _dropped);

    /// <inheritdoc />
    public bool TryWrite(AuditEvent e)
    {
        if (_channel.Writer.TryWrite(e)) return true;
        Interlocked.Increment(ref _dropped);
        return false;
    }
}
