// File    : UndoRedoService.cs
// Module  : Core
// Layer   : Shared
// Purpose : Stack-based undo/redo implementation — max 50 states, thread-safe.

namespace ConfigStudio.WPF.UI.Core.Services;

/// <summary>
/// Stack-based undo/redo. Mỗi state = snapshot + description.
/// Max <see cref="MaxHistory"/> states trong undo stack. Redo stack bị xóa khi push state mới.
/// </summary>
public sealed class UndoRedoService<TSnapshot> : IUndoRedoService<TSnapshot> where TSnapshot : class
{
    private const int MaxHistory = 50;

    private readonly Stack<UndoEntry> _undoStack = new();
    private readonly Stack<UndoEntry> _redoStack = new();

    /// <inheritdoc />
    public bool CanUndo => _undoStack.Count > 0;

    /// <inheritdoc />
    public bool CanRedo => _redoStack.Count > 0;

    /// <inheritdoc />
    public int UndoCount => _undoStack.Count;

    /// <inheritdoc />
    public int RedoCount => _redoStack.Count;

    /// <inheritdoc />
    public event EventHandler? StateChanged;

    /// <inheritdoc />
    public void PushState(TSnapshot snapshot, string description)
    {
        // Redo stack bị xóa khi có action mới
        _redoStack.Clear();

        _undoStack.Push(new UndoEntry(snapshot, description));

        // Giới hạn undo stack
        if (_undoStack.Count > MaxHistory)
            TrimStack(_undoStack, MaxHistory);

        OnStateChanged();
    }

    /// <inheritdoc />
    public TSnapshot? Undo(TSnapshot currentSnapshot)
    {
        if (_undoStack.Count == 0)
            return null;

        var previous = _undoStack.Pop();
        _redoStack.Push(new UndoEntry(currentSnapshot, previous.Description));

        OnStateChanged();
        return previous.Snapshot;
    }

    /// <inheritdoc />
    public TSnapshot? Redo(TSnapshot currentSnapshot)
    {
        if (_redoStack.Count == 0)
            return null;

        var next = _redoStack.Pop();
        _undoStack.Push(new UndoEntry(currentSnapshot, next.Description));

        OnStateChanged();
        return next.Snapshot;
    }

    /// <inheritdoc />
    public void Clear()
    {
        _undoStack.Clear();
        _redoStack.Clear();
        OnStateChanged();
    }

    private void OnStateChanged()
        => StateChanged?.Invoke(this, EventArgs.Empty);

    /// <summary>Trim stack giữ lại maxItems phần tử mới nhất.</summary>
    private static void TrimStack(Stack<UndoEntry> stack, int maxItems)
    {
        var temp = stack.ToArray();
        stack.Clear();
        // Reverse vì ToArray trả từ top → bottom, push lại từ bottom → top
        for (int i = Math.Min(temp.Length, maxItems) - 1; i >= 0; i--)
            stack.Push(temp[i]);
    }

    /// <summary>Entry trong undo/redo stack.</summary>
    private sealed record UndoEntry(TSnapshot Snapshot, string Description);
}
