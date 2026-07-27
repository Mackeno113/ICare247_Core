// File    : ErrorLogQueue.cs
// Module  : Errors
// Layer   : Infrastructure
// Purpose : Cài đặt IErrorLogQueue bằng System.Threading.Channels (bounded, DROP khi đầy).

using System.Threading.Channels;
using ICare247.Application.Interfaces;

namespace ICare247.Infrastructure.Errors;

/// <summary>
/// Hàng đợi lỗi in-memory bounded. <c>FullMode = DropWrite</c> — khi đầy, <see cref="TryWrite"/>
/// trả false ngay (không chặn response), đếm vào <see cref="DroppedCount"/>. Singleton.
/// </summary>
public sealed class ErrorLogQueue : IErrorLogQueue
{
    private readonly Channel<ErrorLogEvent> _channel;
    private long _dropped;

    /// <param name="capacity">Sức chứa tối đa trước khi drop (mặc định 2.000 — lỗi hiếm hơn audit trail nhiều).</param>
    public ErrorLogQueue(int capacity = 2_000)
    {
        _channel = Channel.CreateBounded<ErrorLogEvent>(new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.DropWrite,
            SingleReader = true,
            SingleWriter = false
        });
    }

    /// <inheritdoc />
    public ChannelReader<ErrorLogEvent> Reader => _channel.Reader;

    /// <inheritdoc />
    public long DroppedCount => Interlocked.Read(ref _dropped);

    /// <inheritdoc />
    public bool TryWrite(ErrorLogEvent e)
    {
        if (_channel.Writer.TryWrite(e)) return true;
        Interlocked.Increment(ref _dropped);
        return false;
    }
}
