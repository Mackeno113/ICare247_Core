// File    : RuntimeModels.cs
// Module  : RuntimeCheck
// Purpose : DTOs cho runtime API — UiDelta, ValidationResult, request bodies.

using System.Text.Json.Serialization;

namespace ICare247.Blazor.RuntimeCheck.Models;

// ── Response từ POST /api/v1/forms/{code}/handle-event ───────────────────────

/// <summary>Danh sách UI delta từ EventEngine — áp dụng tuần tự lên form state.</summary>
public sealed class UiDeltaResponseDto
{
    public List<UiDeltaDto> Delta { get; set; } = [];
}

/// <summary>
/// Một delta thay đổi UI.
/// Action: SET_VALUE | SET_VISIBLE | SET_REQUIRED | SET_READONLY |
///         RELOAD_OPTIONS | TRIGGER_VALIDATION.
/// </summary>
public sealed class UiDeltaDto
{
    public string?                     FieldCode { get; set; }
    public string                      Action    { get; set; } = "";
    public Dictionary<string, object?> Data      { get; set; } = [];
}

// ── Response từ POST /api/v1/forms/{code}/validate-field & validate ───────────

/// <summary>Kết quả validate một field đơn lẻ.</summary>
public sealed class FieldValidationResponseDto
{
    public bool                    IsValid  { get; set; } = true;
    public List<ValidationItemDto> Errors   { get; set; } = [];
}

/// <summary>Kết quả validate toàn form: map FieldCode → list lỗi.</summary>
public sealed class FormValidationResponseDto
{
    public bool                                       IsValid { get; set; } = true;
    public Dictionary<string, List<ValidationItemDto>> Fields  { get; set; } = [];
}

public sealed class ValidationItemDto
{
    public int    RuleId   { get; set; }
    public string Severity { get; set; } = "error";
    public string Message  { get; set; } = "";
}

// ── Request bodies ────────────────────────────────────────────────────────────

public sealed class ValidateFieldRequest
{
    [JsonPropertyName("fieldCode")]
    public string FieldCode { get; init; } = "";

    [JsonPropertyName("value")]
    public object? Value { get; init; }

    [JsonPropertyName("contextSnapshot")]
    public Dictionary<string, object?> ContextSnapshot { get; init; } = [];
}

public sealed class ValidateFormRequest
{
    [JsonPropertyName("contextSnapshot")]
    public Dictionary<string, object?> ContextSnapshot { get; init; } = [];
}

public sealed class HandleEventRequest
{
    [JsonPropertyName("eventType")]
    public string EventType { get; init; } = "";

    [JsonPropertyName("sourceField")]
    public string? SourceField { get; init; }

    [JsonPropertyName("contextSnapshot")]
    public Dictionary<string, object?> ContextSnapshot { get; init; } = [];
}

// ── Form runtime state ────────────────────────────────────────────────────────

/// <summary>
/// Trạng thái runtime của một field trong form.
/// Được cập nhật bởi UiDelta từ EventEngine.
/// </summary>
public sealed class FieldState
{
    public string  FieldCode  { get; init; } = "";
    public string  FieldType  { get; init; } = "text";
    public string  Label      { get; set; }  = "";
    public object? Value      { get; set; }
    public bool    IsVisible  { get; set; }  = true;
    public bool    IsRequired { get; set; }
    public bool    IsReadOnly { get; set; }
    public List<string> Errors { get; set; } = [];

    /// <summary>
    /// Mã lookup trong Sys_Lookup — chỉ có giá trị khi LookupSource = "static".
    /// Dùng để load Options qua LookupApiService.
    /// </summary>
    public string? LookupCode { get; init; }

    /// <summary>
    /// Danh sách options cho select field — load từ Sys_Lookup API.
    /// Rỗng khi chưa load hoặc không phải select field.
    /// </summary>
    public List<LookupOptionDto> Options { get; set; } = [];

    /// <summary>Giá trị dạng string để bind vào input element.</summary>
    public string ValueString
    {
        get => Value?.ToString() ?? "";
        set => Value = value;
    }
}

/// <summary>Một option trong danh sách select — map từ Sys_Lookup item.</summary>
public sealed class LookupOptionDto
{
    public string ItemCode  { get; set; } = "";
    public string Label     { get; set; } = "";
    public int    SortOrder { get; set; }
}

/// <summary>Cài đặt API từ appsettings.json.</summary>
public sealed class ApiSettings
{
    public string BaseUrl  { get; set; } = "https://localhost:7001";
    public int    TenantId { get; set; } = 1;
    public string LangCode { get; set; } = "vi";
    public string Platform { get; set; } = "web";
}
