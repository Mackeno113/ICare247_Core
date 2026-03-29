# Spec 11 — Blazor Control Renderer

> Phiên bản: 1.0 | Cập nhật: 2026-03-29

## 1. Nguyên tắc thiết kế

### 1.1 Mỗi EditorType = 1 Renderer riêng

`DxTextBox` và `DxMemo` là 2 control **hoàn toàn khác nhau**. User tự chọn EditorType phù hợp với kiểu dữ liệu trong WPF ConfigStudio. Không có logic tự động switch giữa các control.

| EditorType (WPF) | Renderer (Blazor) | DX Control |
|---|---|---|
| `TextBox` | `TextBoxRenderer` | `DxTextBox` |
| `TextArea` | `MemoRenderer` | `DxMemo` |
| `NumericBox` | `NumericBoxRenderer` | `DxSpinEdit` |
| `DatePicker` | `DatePickerRenderer` | `DxDateEdit` |
| `CheckBox` | `CheckBoxRenderer` | `DxCheckBox` |
| `ToggleSwitch` | `ToggleSwitchRenderer` | `DxCheckBox` (toggle style) |
| `ComboBox` | `ComboBoxRenderer` | `DxComboBox` |
| `RadioGroup` | `RadioGroupRenderer` | `DxRadioGroup` |
| `LookupComboBox` | `LookupComboBoxRenderer` | `DxComboBox` |
| `LookupBox` | `LookupBoxRenderer` | `DxDropDownBox` |

### 1.2 ControlPropsJson pattern

Mỗi field trong DB có cột `Control_Props_Json` (nvarchar max). Renderer tự parse JSON này thành typed props object. Nếu JSON null/rỗng/lỗi → dùng default values.

```json
// TextBox example
{
  "maxLength": 100,
  "isPassword": false,
  "autoComplete": "off",
  "bindValueMode": "OnLostFocus",
  "inputDelay": 300,
  "clearButtonMode": "Auto"
}
```

### 1.3 NullText / Placeholder

- **NullText** trong renderer = `State.Label + "..."` (fallback tạm thời)
- **Đúng cách** (TODO): đọc từ i18n `PlaceholderKey` → `Sys_Resource` → text đã dịch
- **Không** lưu `nullText` hardcode trong `Control_Props_Json` — dùng i18n

### 1.4 Validation display

- Dùng CSS class `dx-has-error` trên container → DX hiển thị border đỏ
- Error text hiển thị qua `FieldRenderer` (`.field-errors` div bên dưới control)
- `ValidationEnabled="true"` + `ValidationStatus` → border state từ DX native

---

## 2. DxTextBox — Spec đầy đủ

**EditorType:** `TextBox` | **Renderer:** `TextBoxRenderer.razor`

### 2.1 Template chuẩn

```razor
<DxTextBox Text="@_localValue"
           TextChanged="@HandleTextChanged"

           BindValueMode="BindValueMode.OnLostFocus"
           InputDelay="0"

           NullText="@NullTextValue"
           CssClass="field-dx"
           Width="100%"
           InputId="field_{FieldCode}"

           Enabled="true"
           ReadOnly="false"

           Password="false"
           MaxLength="255"
           AutoComplete="off"

           ClearButtonDisplayMode="DataEditorClearButtonDisplayMode.Auto" />
```

### 2.2 ControlPropsJson schema

| PropName | Type | Default | Mô tả |
|---|---|---|---|
| `maxLength` | Number | 255 | Độ dài ký tự tối đa |
| `isPassword` | Boolean | false | Ẩn ký tự nhập (password mode) |
| `autoComplete` | String | "off" | HTML autocomplete: "off" / "on" / "new-password" |
| `bindValueMode` | Enum | "OnLostFocus" | "OnLostFocus" (form) hoặc "OnInput" (realtime search) |
| `inputDelay` | Number | 300 | Delay ms khi OnInput, tránh call API quá nhiều |
| `clearButtonMode` | Enum | "Auto" | "Auto" (hiện khi có value) / "Never" |

### 2.3 Best practice

| Trường hợp | BindValueMode | InputDelay | ClearButton |
|---|---|---|---|
| Form nhập liệu | `OnLostFocus` | — | `Auto` |
| Search realtime | `OnInput` | 300–500ms | `Auto` |
| Password | `OnLostFocus` | — | `Never` |

### 2.4 Events

| Event | Khi nào | Mục đích |
|---|---|---|
| `TextChanged` | Theo BindValueMode | Cập nhật FieldState.Value + phát OnChange |
| `@onfocusout` (wrapper) | Rời khỏi control | Phát OnBlur → trigger validation |

---

## 3. DxMemo — Spec đầy đủ

**EditorType:** `TextArea` | **Renderer:** `MemoRenderer.razor`

### 3.1 Template chuẩn

```razor
<DxMemo Text="@_localValue"
        TextChanged="@HandleTextChanged"

        BindValueMode="BindValueMode.OnLostFocus"
        InputDelay="0"

        NullText="@NullTextValue"
        CssClass="field-dx"
        Width="100%"
        InputId="field_{FieldCode}"

        Enabled="true"
        ReadOnly="false"

        Rows="4" />
```

### 3.2 ControlPropsJson schema

| PropName | Type | Default | Mô tả |
|---|---|---|---|
| `maxLength` | Number | 4000 | Độ dài tối đa (nvarchar max field) |
| `rows` | Number | 4 | Số dòng hiển thị |
| `bindValueMode` | Enum | "OnLostFocus" | "OnLostFocus" / "OnInput" |
| `inputDelay` | Number | 300 | Delay ms khi OnInput |

---

## 4. DxCheckBox — Spec đầy đủ

**EditorType:** `CheckBox` / `ToggleSwitch` | **Renderer:** `CheckBoxRenderer.razor`

### 4.1 Mapping EditorType → CheckType

| EditorType (WPF) | FieldType (normalized) | CheckType | Indeterminate |
|---|---|---|---|
| `CheckBox` | `bool` | `CheckType.CheckBox` | Tùy `allowIndeterminate` prop |
| `ToggleSwitch` | `switch` | `CheckType.Switch` | Không hỗ trợ |

> ToggleSwitch với `AllowIndeterminateState=true` → indeterminate bị coi là unchecked.

### 4.2 Template chuẩn

```razor
@* CheckBox thường *@
<DxCheckBox @bind-Checked="@Model.IsActive"
            CheckType="CheckType.CheckBox"
            AllowIndeterminateState="false"
            CssClass="field-dx"
            Alignment="CheckBoxContentAlignment.Center"
            LabelPosition="LabelPosition.Right"
            LabelWrapMode="LabelWrapMode.WordWrap"
            Enabled="true"
            ReadOnly="false"
            ValidationEnabled="true"
            InputId="chkIsActive">
    Kích hoạt
</DxCheckBox>

@* Switch mode *@
<DxCheckBox @bind-Checked="@Model.IsEnabled"
            CheckType="CheckType.Switch"
            LabelPosition="LabelPosition.Right"
            Enabled="true"
            InputId="chkIsEnabled">
    Bật / Tắt
</DxCheckBox>

@* 3 trạng thái (bool?) *@
<DxCheckBox @bind-Checked="@Model.ApprovedState"
            AllowIndeterminateState="true"
            AllowIndeterminateStateByClick="true"
            CheckType="CheckType.CheckBox"
            LabelPosition="LabelPosition.Right"
            InputId="chkApproved">
    Trạng thái duyệt
</DxCheckBox>
```

### 4.3 ControlPropsJson schema

**CheckBox:**

| PropName | Type | Default | Mô tả |
|---|---|---|---|
| `allowIndeterminate` | Boolean | false | 3 trạng thái: `true/false/null` |
| `labelPosition` | Enum | "Right" | "Right" / "Left" |
| `labelWrapMode` | Enum | "WordWrap" | "WordWrap" / "Ellipsis" / "NoWrap" |

**ToggleSwitch:**

| PropName | Type | Default | Mô tả |
|---|---|---|---|
| `labelPosition` | Enum | "Right" | "Right" / "Left" |

### 4.4 Click order (3 trạng thái)

```
Indeterminate → Checked → Unchecked → Indeterminate → ...
```

### 4.5 Best practice

| Trường hợp | CheckType | AllowIndeterminate |
|---|---|---|
| Yes/No thông thường | `CheckBox` | false |
| Bật/tắt kiểu mobile | `Switch` | false |
| Trạng thái chưa xác định | `CheckBox` | true |
| Form readonly | `ReadOnly="true"` | — |
| Khóa hoàn toàn | `Enabled="false"` | — |

> **Lưu ý:** `Switch` không hỗ trợ indeterminate — dùng `CheckBox` nếu cần 3 trạng thái.

### 4.6 Label

- Label text lấy từ `State.Label` (ChildContent của DxCheckBox)
- `FieldRenderer` hiển thị label trên không cần thiết cho CheckBox → có thể ẩn sau
- Required mark (`*`) được append vào ChildContent text

---

## 5. Pattern chung cho mọi Renderer

### 4.1 File structure

```
Components/FieldRenderers/
├── TextBoxRenderer.razor      ← DxTextBox
├── MemoRenderer.razor         ← DxMemo
├── NumericBoxRenderer.razor   ← DxSpinEdit
├── DatePickerRenderer.razor   ← DxDateEdit
├── CheckBoxRenderer.razor     ← DxCheckBox
├── ComboBoxRenderer.razor     ← DxComboBox (dynamic)
├── LookupBoxRenderer.razor    ← DxDropDownBox (FK popup)
└── RadioGroupRenderer.razor   ← DxRadioGroup
```

### 4.2 Parameters chuẩn

```csharp
[Parameter, EditorRequired] public FieldState? State { get; set; }
[Parameter] public EventCallback<(string Code, object? Value)> OnChange { get; set; }
[Parameter] public EventCallback<string> OnBlur { get; set; }
```

### 4.3 Code pattern chuẩn

```csharp
private XxxProps _props = new();
private T _localValue = default!;   // sync từ State.ValueString

protected override void OnParametersSet()
{
    _props = XxxProps.Parse(State?.ControlPropsJson);
    _localValue = ...; // parse từ State.Value
}

private async Task HandleValueChanged(T value)
{
    if (State is null) return;
    _localValue = value;
    State.Value = value;
    await OnChange.InvokeAsync((State.FieldCode, value));
}

private async Task HandleLostFocus(FocusEventArgs _)
{
    if (State is null) return;
    await OnBlur.InvokeAsync(State.FieldCode);
}
```

### 4.4 Props class pattern

```csharp
private sealed class XxxProps
{
    [JsonPropertyName("propName")]
    public Type PropName { get; set; } = defaultValue;

    public static XxxProps Parse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new();
        try { return JsonSerializer.Deserialize<XxxProps>(json,
                  new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new(); }
        catch { return new(); }
    }
}
```

---

## 5. FieldRenderer — Routing table

`FieldRenderer.razor` switch theo `State.FieldType` (đã normalize bởi `FormRunner.NormalizeFieldType`):

| FieldType (normalized) | Renderer |
|---|---|
| `"text"` | `TextBoxRenderer` |
| `"textarea"` | `MemoRenderer` |
| `"number"` | `NumericBoxRenderer` _(pending)_ |
| `"date"` | `DatePickerRenderer` _(pending)_ |
| `"datetime"` | `DatePickerRenderer` _(pending)_ |
| `"bool"` | `CheckBoxRenderer` _(pending)_ |
| `"select"` | HTML `<select>` (static Sys_Lookup) |
| `"combobox"` | `ComboBoxRenderer` |
| `"fklookup"` | `LookupBoxRenderer` |
| _(default)_ | `TextBoxRenderer` |

---

## 6. NormalizeFieldType mapping

`FormRunner.razor` → `NormalizeFieldType(dbType, lookupSource)`:

| DB EditorType | LookupSource | Normalized |
|---|---|---|
| TextBox / Text | — | `text` |
| TextArea / Memo | — | `textarea` |
| NumericBox / NumberEdit | — | `number` |
| DateEdit / Date | — | `date` |
| DateTimeEdit / DateTime | — | `datetime` |
| CheckBox / Toggle | — | `bool` |
| ComboBox | `dynamic` | `combobox` |
| ComboBox | `static` / null | `select` |
| RadioGroup / LookupComboBox | — | `select` |
| LookupBox | — | `fklookup` |
