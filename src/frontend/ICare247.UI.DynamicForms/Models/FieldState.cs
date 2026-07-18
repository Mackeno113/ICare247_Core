// File    : FieldState.cs
// Module  : ICare247.UI.DynamicForms
// Purpose : Trạng thái runtime của một field trong form — dùng chung cho mọi host (Admin/Portal).
//           Được cập nhật bởi UiDelta từ EventEngine. Tách từ ICare247_UI.Models (RCL DynamicForms).

namespace ICare247.UI.DynamicForms.Models;

/// <summary>
/// Trạng thái runtime của một field trong form.
/// Được cập nhật bởi UiDelta từ EventEngine.
/// </summary>
public sealed class FieldState
{
    /// <summary>Field_Id trong Ui_Field — dùng để gọi POST /api/v1/lookups/query-dynamic.</summary>
    public int     FieldId   { get; init; }
    public string  FieldCode  { get; init; } = "";
    public string  FieldType  { get; init; } = "text";
    public string  Label      { get; set; }  = "";
    public object? Value      { get; set; }
    public bool    IsVisible  { get; set; }  = true;
    public bool    IsRequired { get; set; }
    public bool    IsReadOnly { get; set; }
    /// <summary>true = khóa khi FormMode=Edit (ADR-017). Effective ReadOnly = IsReadOnly OR (LockOnEdit AND IsEditMode).</summary>
    public bool    LockOnEdit { get; set; }
    /// <summary>true = field UI-only, không map cột DB. BuildContext vẫn include (cần cho rule eval), save layer loại bỏ.</summary>
    public bool    IsVirtual  { get; init; }

    /// <summary>
    /// true = field bị một control composite (VD AddressBox) render THAY → host KHÔNG render riêng.
    /// Vẫn nằm trong payload Lưu (IsVisible &amp;&amp; !IsVirtual) để cột companion được ghi bình thường.
    /// Được set bởi host sau khi dựng field states (quét field neo AddressBox → đánh dấu cột text đi kèm).
    /// </summary>
    public bool    IsHiddenByComposite { get; set; }

    /// <summary>
    /// Form đang ở chế độ Edit (record đã tồn tại, RecordId > 0). Cùng giá trị cho mọi FieldState
    /// của 1 form — copy từ FormRunner. Dùng để compute EffectiveReadOnly với LockOnEdit.
    /// </summary>
    public bool    IsEditMode { get; set; }

    /// <summary>
    /// ADR-017: Trạng thái ReadOnly hiệu lực sau khi áp dụng quy tắc Lock_On_Edit.
    /// Renderer dùng cờ này thay cho IsReadOnly để hiển thị/disable input.
    /// </summary>
    public bool EffectiveReadOnly => IsReadOnly || (LockOnEdit && IsEditMode);
    public List<string> Errors { get; set; } = [];

    /// <summary>null | "static" | "dynamic" — phân loại nguồn dữ liệu lookup.</summary>
    public string? LookupSource { get; init; }

    /// <summary>
    /// Mã lookup trong Sys_Lookup — chỉ có giá trị khi LookupSource = "static".
    /// Dùng để load Options qua LookupApiService.
    /// </summary>
    public string? LookupCode { get; init; }

    /// <summary>
    /// Danh sách options cho static select field — load từ Sys_Lookup API.
    /// Rỗng khi chưa load hoặc không phải select field.
    /// </summary>
    public List<LookupOptionDto> Options { get; set; } = [];

    /// <summary>
    /// Cấu hình dynamic lookup — chỉ có giá trị khi LookupSource = "dynamic".
    /// Dùng bởi ComboBoxRenderer / LookupBoxRenderer để biết ValueColumn, DisplayColumn, v.v.
    /// </summary>
    public FieldLookupConfigDto? LookupConfig { get; init; }

    /// <summary>
    /// Rows dữ liệu dynamic lookup đã load — cache trong session.
    /// Cleared + reloaded khi ReloadTriggerField thay đổi giá trị.
    /// </summary>
    public List<Dictionary<string, object?>> DynamicRows { get; set; } = [];

    /// <summary>
    /// Độ rộng field trong CSS grid 4-column (grid-column: span X).
    /// 1 = 1/4, 2 = 2/4(half), 3 = 3/4, 4 = full.
    /// </summary>
    public byte ColSpan { get; init; } = 1;

    /// <summary>
    /// JSON cấu hình UI component từ DB (Ui_Field.Control_Props_Json).
    /// Renderer tự parse theo FieldType: TextBox→TextBoxProps, v.v.
    /// </summary>
    public string? ControlPropsJson { get; init; }

    /// <summary>Giá trị dạng string để bind vào input element.</summary>
    public string ValueString
    {
        get => Value?.ToString() ?? "";
        set => Value = value;
    }
}
