// File    : TabUpsertRequest.cs
// Module  : Data
// Layer   : Core
// Purpose : Request model cho UpsertTabAsync — tạo mới hoặc cập nhật Ui_Tab + rename Resource_Key khi đổi Tab Code.

namespace ConfigStudio.WPF.UI.Core.Data;

/// <summary>
/// Dữ liệu cần thiết để upsert một Tab vào DB.
/// Khi <see cref="TabId"/> = 0 → INSERT mới, trả về Tab_Id được sinh.
/// Khi <see cref="TabId"/> &gt; 0 → UPDATE bản ghi hiện có.
/// Khi <see cref="OldTitleKey"/> khác <see cref="TitleKey"/> → rename Resource_Key trong Sys_Resource.
/// Khi <see cref="IsDefault"/> = true → các tab khác cùng form bị gỡ cờ default (max 1 default/form).
/// </summary>
public sealed record TabUpsertRequest(
    int     FormId,
    int     TabId,
    string  TabCode,
    string  TitleKey,
    string? IconKey,
    int     OrderNo,
    bool    IsDefault,
    string  OldTitleKey
);
