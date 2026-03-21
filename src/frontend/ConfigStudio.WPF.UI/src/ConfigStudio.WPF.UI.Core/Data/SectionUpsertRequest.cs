// File    : SectionUpsertRequest.cs
// Module  : Data
// Layer   : Core
// Purpose : Request model cho UpsertSectionAsync — tạo mới hoặc cập nhật Ui_Section + rename Resource_Key khi đổi Section Code.

namespace ConfigStudio.WPF.UI.Core.Data;

/// <summary>
/// Dữ liệu cần thiết để upsert một Section vào DB.
/// Khi <see cref="SectionId"/> = 0 → INSERT mới, trả về Section_Id được sinh.
/// Khi <see cref="SectionId"/> > 0 → UPDATE bản ghi hiện có.
/// Khi <see cref="OldTitleKey"/> khác <see cref="TitleKey"/> → rename Resource_Key trong Sys_Resource.
/// </summary>
public sealed record SectionUpsertRequest(
    int    FormId,
    int    SectionId,
    string SectionCode,
    string TitleKey,
    int    OrderNo,
    bool   IsActive,
    string OldTitleKey
);
