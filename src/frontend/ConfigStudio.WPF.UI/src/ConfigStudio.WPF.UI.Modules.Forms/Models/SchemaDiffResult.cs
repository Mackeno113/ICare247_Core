// File    : SchemaDiffResult.cs
// Module  : Forms
// Layer   : Presentation
// Purpose : Kết quả so sánh cấu trúc cột Target DB với danh sách field hiện có trong form.
//           Dùng trong CheckSchemaDiffAsync và SyncSchemaDialog.

using ConfigStudio.WPF.UI.Core.Data;

namespace ConfigStudio.WPF.UI.Modules.Forms.Models;

/// <summary>
/// Chứa kết quả diff giữa Target DB schema và Ui_Field hiện tại của form.
/// </summary>
public sealed class SchemaDiffResult
{
    /// <summary>
    /// Cột có trong Target DB nhưng chưa có field tương ứng trong form.
    /// Người dùng có thể chọn để tạo thêm field.
    /// </summary>
    public IReadOnlyList<ColumnSchemaDto> ColumnsToAdd { get; init; } = [];

    /// <summary>
    /// Field trong form có Code không tồn tại trong Target DB
    /// (có thể cột đã bị đổi tên hoặc xóa).
    /// </summary>
    public IReadOnlyList<FormTreeNode> OrphanedFields { get; init; } = [];

    /// <summary>
    /// Field có EditorType không còn phù hợp với DataType hiện tại của cột.
    /// Ví dụ: cột đổi từ nvarchar → bit nhưng field vẫn là TextBox.
    /// </summary>
    public IReadOnlyList<TypeMismatchItem> TypeMismatches { get; init; } = [];

    /// <summary>Tổng số issue cần chú ý (orphan + mismatch). Không đếm ColumnsToAdd vì đây là gợi ý.</summary>
    public int IssueCount => OrphanedFields.Count + TypeMismatches.Count;

    /// <summary>Có bất kỳ sự khác biệt nào không (kể cả gợi ý thêm cột).</summary>
    public bool HasAnyDiff => ColumnsToAdd.Count > 0 || IssueCount > 0;

    /// <summary>Empty result — dùng khi Target DB chưa cấu hình hoặc không compare được.</summary>
    public static SchemaDiffResult Empty { get; } = new();
}

/// <summary>
/// Một field có EditorType không khớp với DataType hiện tại của cột Target DB.
/// </summary>
public sealed record TypeMismatchItem(
    FormTreeNode Field,
    ColumnSchemaDto TargetColumn,
    string SuggestedEditorType)
{
    public string Description =>
        $"Field '{Field.Code}' đang dùng {Field.EditorType} " +
        $"nhưng cột '{TargetColumn.ColumnName}' có kiểu {TargetColumn.DataType} " +
        $"→ gợi ý: {SuggestedEditorType}";
}
