// File    : IUndoRedoService.cs
// Module  : Core
// Layer   : Shared
// Purpose : Interface cho Undo/Redo service — lưu trạng thái và khôi phục theo stack.

namespace ConfigStudio.WPF.UI.Core.Services;

/// <summary>
/// Service quản lý undo/redo cho một editor context.
/// Mỗi editor (FormEditor, FieldConfig) dùng instance riêng qua factory.
/// </summary>
/// <typeparam name="TSnapshot">Kiểu snapshot trạng thái cần lưu.</typeparam>
public interface IUndoRedoService<TSnapshot> where TSnapshot : class
{
    /// <summary>Có thể Undo không.</summary>
    bool CanUndo { get; }

    /// <summary>Có thể Redo không.</summary>
    bool CanRedo { get; }

    /// <summary>Số operations trong undo stack.</summary>
    int UndoCount { get; }

    /// <summary>Số operations trong redo stack.</summary>
    int RedoCount { get; }

    /// <summary>
    /// Lưu snapshot hiện tại vào undo stack. Xóa redo stack (action mới phá redo history).
    /// </summary>
    /// <param name="snapshot">Snapshot trạng thái cần lưu.</param>
    /// <param name="description">Mô tả ngắn action (hiển thị tooltip, audit).</param>
    void PushState(TSnapshot snapshot, string description);

    /// <summary>
    /// Undo: pop undo stack → push current vào redo stack → trả snapshot trước đó.
    /// </summary>
    /// <param name="currentSnapshot">Snapshot hiện tại (push vào redo stack).</param>
    /// <returns>Snapshot cần khôi phục. Null nếu undo stack rỗng.</returns>
    TSnapshot? Undo(TSnapshot currentSnapshot);

    /// <summary>
    /// Redo: pop redo stack → push current vào undo stack → trả snapshot tiếp theo.
    /// </summary>
    /// <param name="currentSnapshot">Snapshot hiện tại (push vào undo stack).</param>
    /// <returns>Snapshot cần áp dụng. Null nếu redo stack rỗng.</returns>
    TSnapshot? Redo(TSnapshot currentSnapshot);

    /// <summary>Xóa toàn bộ undo/redo history (sau save thành công).</summary>
    void Clear();

    /// <summary>Event khi CanUndo hoặc CanRedo thay đổi.</summary>
    event EventHandler? StateChanged;
}
