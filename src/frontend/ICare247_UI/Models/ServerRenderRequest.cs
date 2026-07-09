// File    : ServerRenderRequest.cs
// Module  : ICare247_UI
// Purpose : Yêu cầu xuất tài liệu server-side từ nút Ui_View_Action (Export/Print + Engine=Server).
//           DataView gom (mã bộ mẫu + định dạng + dòng đang chọn) → ViewPage gọi DocTemplateApiService.

namespace ICare247_UI.Models;

/// <summary>
/// Dữ liệu 1 lần bấm nút xuất tài liệu theo mẫu trên lưới. <see cref="Row"/> là dòng đang chọn
/// (đầy đủ cột) — dùng làm keyParams; rỗng nghĩa là người dùng chưa chọn dòng nào.
/// </summary>
public sealed record ServerRenderRequest(
    string TemplateCode,
    string Format,
    IReadOnlyDictionary<string, object?> Row,
    bool HasSelection);
