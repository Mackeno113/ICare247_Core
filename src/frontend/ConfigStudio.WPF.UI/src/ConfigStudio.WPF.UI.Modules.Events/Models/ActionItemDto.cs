// File    : ActionItemDto.cs
// Module  : Events
// Layer   : Presentation
// Purpose : DTO hiển thị 1 action trong danh sách actions của event.
//           Hỗ trợ structured editor cho SHOW_MESSAGE (messageKey + severity ↔ ParamJson).

using System.Text.Json;
using Prism.Mvvm;

namespace ConfigStudio.WPF.UI.Modules.Events.Models;

/// <summary>
/// DTO đại diện cho 1 action thuộc event.
/// </summary>
public class ActionItemDto : BindableBase
{
    public int ActionId { get; set; }
    public int EventId { get; set; }

    private string _actionType = "";
    /// <summary>Loại action: SET_VALUE, SET_VISIBLE, SHOW_MESSAGE, ...</summary>
    public string ActionType
    {
        get => _actionType;
        set { if (SetProperty(ref _actionType, value)) RaisePropertyChanged(nameof(IsShowMessage)); }
    }

    private string _targetField = "";
    /// <summary>Field mục tiêu của action.</summary>
    public string TargetField { get => _targetField; set => SetProperty(ref _targetField, value); }

    private string _paramJson = "";
    /// <summary>JSON chứa tham số của action.</summary>
    public string ParamJson { get => _paramJson; set => SetProperty(ref _paramJson, value); }

    private int _orderNo;
    public int OrderNo { get => _orderNo; set => SetProperty(ref _orderNo, value); }

    /// <summary>Mô tả ngắn action.</summary>
    public string DisplayText => $"{ActionType} → {TargetField}";

    // ── SHOW_MESSAGE structured params ───────────────────────

    /// <summary>True khi action là SHOW_MESSAGE → hiện structured editor.</summary>
    public bool IsShowMessage => string.Equals(ActionType, "SHOW_MESSAGE", StringComparison.OrdinalIgnoreCase);

    private string _messageKey = "";
    /// <summary>SHOW_MESSAGE: resource key của nội dung thông báo (đa ngôn ngữ).</summary>
    public string MessageKey { get => _messageKey; set => SetProperty(ref _messageKey, value); }

    private string _severity = "info";
    /// <summary>SHOW_MESSAGE: mức độ — info / warn / error.</summary>
    public string Severity { get => _severity; set => SetProperty(ref _severity, value); }

    /// <summary>
    /// Parse <see cref="ParamJson"/> → điền các field structured (messageKey, severity, targetField).
    /// Gọi sau khi load action từ DB. Bỏ qua JSON lỗi định dạng.
    /// </summary>
    public void LoadParamsFromJson()
    {
        if (string.IsNullOrWhiteSpace(ParamJson)) return;
        try
        {
            using var doc = JsonDocument.Parse(ParamJson);
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object) return;

            if (root.TryGetProperty("messageKey", out var mk) && mk.ValueKind == JsonValueKind.String)
                MessageKey = mk.GetString() ?? "";
            if (root.TryGetProperty("severity", out var sv) && sv.ValueKind == JsonValueKind.String)
                Severity = sv.GetString() ?? "info";
            if (string.IsNullOrEmpty(TargetField)
                && root.TryGetProperty("targetField", out var tf) && tf.ValueKind == JsonValueKind.String)
                TargetField = tf.GetString() ?? "";
        }
        catch (JsonException) { /* JSON lỗi → giữ nguyên field rỗng */ }
    }

    /// <summary>
    /// Serialize field structured của SHOW_MESSAGE về <see cref="ParamJson"/>.
    /// Chỉ áp dụng cho SHOW_MESSAGE — các action khác giữ nguyên ParamJson.
    /// Gọi trước khi save xuống DB.
    /// </summary>
    public void SyncParamsToJson()
    {
        if (!IsShowMessage) return;
        ParamJson = JsonSerializer.Serialize(new { messageKey = MessageKey, severity = Severity });
    }
}
