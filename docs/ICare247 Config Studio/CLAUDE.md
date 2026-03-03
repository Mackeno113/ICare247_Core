# ICare247 Config Studio — Project Memory

> **File này được đặt ở root của project.
> Claude Code sẽ tự đọc mỗi khi bắt đầu session mới.**

---

## 1. Mục Tiêu Project

Config Studio là **WPF desktop authoring tool** để tạo/quản lý metadata cho ICare247 Form Engine.
Không viết code business logic — chỉ cấu hình form, field, rule, event vào DB.

---

## 2. Tech Stack

| Thành phần | Package | Ghi chú |
|---|---|---|
| WPF Framework | .NET 9 / `net9.0-windows` | `<UseWPF>true</UseWPF>` |
| DI + Navigation | `Prism.Unity` 9.x | `PrismApplication`, `IRegionManager` |
| UI Toolkit | `MaterialDesignThemes` 5.x | Dark theme, Indigo/LightBlue |
| MVVM | Prism `BindableBase` + `DelegateCommand` | KHÔNG dùng CommunityToolkit |
| Icons | `MaterialDesignThemes` (PackIcon) | MaterialDesignKind enum |

---

## 3. Solution Structure

```
ICare247.ConfigStudio.sln
├── src/
│   ├── ICare247.ConfigStudio/              ← WPF Shell App (startup project)
│   ├── ICare247.ConfigStudio.Core/         ← Shared: Constants, Base classes, Models
│   ├── ICare247.ConfigStudio.Modules.Forms/   ← Module: Form Manager, Form Editor
│   ├── ICare247.ConfigStudio.Modules.Rules/   ← Module: Validation Rule Editor
│   ├── ICare247.ConfigStudio.Modules.Events/  ← Module: Event Editor
│   ├── ICare247.ConfigStudio.Modules.Grammar/ ← Module: Expression Builder, Grammar Library
│   └── ICare247.ConfigStudio.Modules.I18n/    ← Module: i18n Resource Manager
```

---

## 4. Quy Tắc Đặt Tên (BẮT BUỘC)

```
View:          [Name]View.xaml           → Views/FormManagerView.xaml
ViewModel:     [Name]ViewModel.cs        → ViewModels/FormManagerViewModel.cs
Module:        [Module]Module.cs         → FormsModule.cs
Dialog:        [Name]Dialog.xaml         → Views/ExpressionBuilderDialog.xaml
Converter:     [Name]Converter.cs        → Converters/BoolToVisibilityConverter.cs
```

### ViewModel phải:
- Kế thừa `ViewModelBase` (từ `ICare247.ConfigStudio.Core`)
- `ViewModelBase` kế thừa `BindableBase` (Prism)
- Dùng `SetProperty(ref _field, value)` cho properties
- Dùng `DelegateCommand` / `DelegateCommand<T>` cho commands
- Implement `INavigationAware` khi nhận params từ navigation

### View phải:
- Không có code-behind logic (chỉ `InitializeComponent()`)
- DataContext bind qua ViewModelLocator (Prism auto-wire): `prism:ViewModelLocator.AutoWireViewModel="True"`

---

## 5. Region Names

```csharp
// ICare247.ConfigStudio.Core/Constants/RegionNames.cs
public static class RegionNames
{
    public const string Content   = "ContentRegion";   // vùng chính giữa Shell
    public const string StatusBar = "StatusBarRegion"; // status bar
    public const string Dialog    = "DialogRegion";    // overlay dialog
}
```

---

## 6. View Names (dùng khi navigate)

```csharp
// ICare247.ConfigStudio.Core/Constants/ViewNames.cs
public static class ViewNames
{
    public const string Dashboard           = nameof(Dashboard);
    public const string FormManager         = nameof(FormManager);
    public const string FormEditor          = nameof(FormEditor);
    public const string FieldConfig         = nameof(FieldConfig);
    public const string ValidationRuleEditor = nameof(ValidationRuleEditor);
    public const string EventEditor         = nameof(EventEditor);
    public const string ExpressionBuilder   = nameof(ExpressionBuilder);
    public const string DependencyViewer    = nameof(DependencyViewer);
    public const string GrammarLibrary      = nameof(GrammarLibrary);
    public const string I18nManager         = nameof(I18nManager);
    public const string PublishChecklist    = nameof(PublishChecklist);
}
```

---

## 7. Pattern Điều Hướng (Navigation)

```csharp
// Navigate đơn giản (không có params)
_regionManager.RequestNavigate(RegionNames.Content, ViewNames.FormManager);

// Navigate với params
var p = new NavigationParameters
{
    { "formId", selectedForm.FormId },
    { "mode",   "edit" }
};
_regionManager.RequestNavigate(RegionNames.Content, ViewNames.FormEditor, p);

// ViewModel nhận params
public void OnNavigatedTo(NavigationContext context)
{
    var formId = context.Parameters.GetValue<int>("formId");
}
```

---

## 8. Pattern Đăng Ký View trong Module

```csharp
// FormsModule.cs
public class FormsModule : IModule
{
    private readonly IRegionManager _rm;
    public FormsModule(IRegionManager rm) => _rm = rm;

    public void OnInitialized(IContainerProvider cp)
    {
        // Navigate mặc định khi module load xong
        _rm.RequestNavigate(RegionNames.Content, ViewNames.FormManager);
    }

    public void RegisterTypes(IContainerRegistry cr)
    {
        cr.RegisterForNavigation<FormManagerView, FormManagerViewModel>(ViewNames.FormManager);
        cr.RegisterForNavigation<FormEditorView,  FormEditorViewModel>(ViewNames.FormEditor);
    }
}
```

---

## 9. DB Schema Tóm Tắt (tham chiếu khi tạo DTO/Model)

### UI Group
| Bảng | Cột chính |
|---|---|
| `Ui_Form` | Form_Id, Form_Code, Table_Id, Platform, Layout_Engine, Is_Active |
| `Ui_Section` | Section_Id, Form_Id, Section_Code, Title_Key, Order_No |
| `Ui_Field` | Field_Id, Form_Id, Section_Id, Column_Id, Editor_Type, Label_Key, Placeholder_Key, Is_Visible, Is_ReadOnly, Order_No, Control_Props_Json |
| `Ui_Control_Map` | Editor_Type, Platform, Control_Name, Default_Props_Json |

### Validation Group
| Bảng | Cột chính |
|---|---|
| `Val_Rule` | Rule_Id, Rule_Type_Code, Error_Key, Expression_Json, Condition_Expr, Is_Active |
| `Val_Rule_Field` | Rule_Field_Id, Field_Id, Rule_Id, Order_No |
| `Val_Rule_Type` | Rule_Type_Code, Param_Schema |

### Event Group
| Bảng | Cột chính |
|---|---|
| `Evt_Definition` | Event_Id, Form_Id, Field_Id, Trigger_Code, Condition_Expr, Order_No |
| `Evt_Action` | Action_Id, Event_Id, Action_Code, Action_Param_Json, Order_No |
| `Evt_Action_Type` | Action_Code, Param_Schema |
| `Evt_Trigger_Type` | Trigger_Code |

### Grammar Group
| Bảng | Cột chính |
|---|---|
| `Gram_Function` | Function_Id, Function_Code, Description, Return_Net_Type, Param_Count_Min, Param_Count_Max |
| `Gram_Operator` | Operator_Symbol, Operator_Type, Precedence, Description |

### System Group
| Bảng | Cột chính |
|---|---|
| `Sys_Column` | Column_Id, Table_Id, Column_Code, Data_Type, Net_Type, Is_Nullable |
| `Sys_Table` | Table_Id, Table_Code, Table_Name |
| `Sys_Resource` | Resource_Key, Lang_Code, Resource_Value |
| `Sys_Dependency` | Source_Type, Source_Id, Target_Type, Target_Id, Form_Id |

---

## 10. Grammar V1 — 6 Rule Types

| Code | Quick Config | Expression_Json | Condition_Expr |
|---|---|---|---|
| `Required` | Không có | NULL (built-in) | Optional |
| `Numeric` | Min, Max inputs | Auto-gen Binary `&&` `>=` `<=` | Optional |
| `Regex` | Pattern textbox | Auto-gen Function `regex(field, pat)` | Optional |
| `Compare` | Operator + Field/Value | Auto-gen Binary operator | Optional |
| `Conditional` | Expression Builder | Manual AST | Bắt buộc |
| `Custom` | HandlerCode dropdown | `{"type":"CustomHandler","handlerCode":"..."}` | Optional |

## 11. 6 Action Types cho Event

| Code | Params | UI Input |
|---|---|---|
| `ShowField` | `fieldCode` | DropDown field |
| `HideField` | `fieldCode` | DropDown field |
| `SetReadOnly` | `fieldCode`, `readOnly: bool` | DropDown + Toggle |
| `SetValue` | `fieldCode`, `valueExpr` (Literal/Identifier only) | DropDown + Literal or Field picker |
| `Calculate` | `targetField`, `expression` (full AST) | DropDown + Expression Builder |
| `CallAPI` | `url`, `method`, `headers`, `bodyExpr`, `mapResponse`, `timeoutMs` | URL input + Method + Mapping grid |

---

## 12. Grammar V1 AST Node Types

```json
// Literal
{ "type": "Literal", "value": 123, "netType": "Int32" }

// Identifier (tham chiếu field theo Column_Code)
{ "type": "Identifier", "name": "SoLuong" }

// Binary
{ "type": "Binary", "operator": ">=", "left": {...}, "right": {...} }

// Unary
{ "type": "Unary", "operator": "!", "operand": {...} }

// Function
{ "type": "Function", "name": "round", "arguments": [{...}, {...}] }
```

**Operators:** `==` `!=` `>` `>=` `<` `<=` `&&` `||` `!` `+` `-` `*` `/` `%` `??`
**Max depth:** 20
**Return type condition/validation:** phải là `Boolean` hoặc `NULL`

---

## 13. MaterialDesign Style Conventions

```xml
<!-- Button styles -->
Style="{StaticResource MaterialDesignRaisedButton}"       ← primary action
Style="{StaticResource MaterialDesignOutlinedButton}"     ← secondary
Style="{StaticResource MaterialDesignFlatButton}"         ← text only / toolbar

<!-- TextBox -->
Style="{StaticResource MaterialDesignOutlinedTextBox}"
materialDesign:HintAssist.Hint="Nhập giá trị..."

<!-- DataGrid -->
Style="{StaticResource MaterialDesignDataGrid}"

<!-- Card container -->
Style="{StaticResource MaterialDesignCardGroupBox}"

<!-- Icon -->
<materialDesign:PackIcon Kind="CheckCircle" Width="20" Height="20"/>
```

---

## 14. Quy Tắc Quan Trọng Khi Code

- **Không hardcode string** — dùng constants trong Core
- **Không navigate trực tiếp trong View** — dùng command trong ViewModel
- **Không new ViewModel trong View** — dùng AutoWireViewModel
- **Không gọi API trong ViewModel constructor** — gọi trong `OnNavigatedTo` hoặc `LoadedCommand`
- **ConfirmDialog trước khi xóa** — dùng `IDialogService` của Prism
- **Validate metadata trước khi Save** — hiện lỗi inline, không MessageBox