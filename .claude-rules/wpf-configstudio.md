# WPF ConfigStudio Rules — ICare247

## Tech Stack

| Thành phần    | Package                | Ghi chú                          |
| ------------- | ---------------------- | -------------------------------- |
| WPF Framework | .NET 9 / `net9.0-windows` | `<UseWPF>true</UseWPF>`      |
| DI + Nav      | `Prism.Unity` 9.x     | `PrismApplication`, `IRegionManager` |
| UI Toolkit    | `MaterialDesignThemes` 5.x | Dark theme, Indigo/LightBlue |
| MVVM          | Prism `BindableBase` + `DelegateCommand` | KHÔNG dùng CommunityToolkit |

## Naming

```
View:      [Name]View.xaml       → Views/FormManagerView.xaml
ViewModel: [Name]ViewModel.cs    → ViewModels/FormManagerViewModel.cs
Module:    [Module]Module.cs     → FormsModule.cs
Dialog:    [Name]Dialog.xaml     → Views/ExpressionBuilderDialog.xaml
Converter: [Name]Converter.cs    → Converters/BoolToVisibilityConverter.cs
```

## ViewModel Rules

- Kế thừa `ViewModelBase` (từ Core) → `BindableBase` (Prism)
- Dùng `SetProperty(ref _field, value)` cho properties
- Dùng `DelegateCommand` / `DelegateCommand<T>` cho commands
- Implement `INavigationAware` khi nhận params từ navigation

## View Rules

- Không có code-behind logic (chỉ `InitializeComponent()`)
- DataContext qua `prism:ViewModelLocator.AutoWireViewModel="True"`

## Navigation

```csharp
// Navigate đơn giản
_regionManager.RequestNavigate(RegionNames.Content, ViewNames.FormManager);

// Navigate với params
var p = new NavigationParameters { { "formId", selectedForm.FormId } };
_regionManager.RequestNavigate(RegionNames.Content, ViewNames.FormEditor, p);
```

## Module Registration

```csharp
public void RegisterTypes(IContainerRegistry cr)
{
    cr.RegisterForNavigation<FormManagerView, FormManagerViewModel>(ViewNames.FormManager);
}
```

## Quy tắc cứng

- **Không hardcode string** — dùng constants trong Core
- **Không navigate trong View** — dùng command trong ViewModel
- **Không new ViewModel trong View** — dùng AutoWireViewModel
- **Không gọi API trong constructor** — gọi trong `OnNavigatedTo` hoặc `LoadedCommand`
- **ConfirmDialog trước khi xóa** — dùng `IDialogService` của Prism

## Prism 9 Breaking Changes (đã gặp)

- Dùng `Prism.Navigation.Regions` thay `Prism.Regions`
- Dùng `Prism.Dialogs` thay `Prism.Services.Dialogs`

---

## Nguyên tắc Thiết kế UI (Design Guidelines)

### 1. Kiến trúc màn hình — không dùng Dialog cho CRUD

- **KHÔNG mở Dialog riêng** cho tạo mới / sửa → dùng **cùng một View**, phân biệt bằng `IsNewForm` flag
- Tạo mới và sửa **phải dùng chung layout, chung fields, chung tab** → đảm bảo nhất quán
- Chỉ dùng Dialog cho: xác nhận xóa (`ConfirmDialog`), chọn phức tạp (`ExpressionBuilder`), hoặc popup nhỏ
- Nếu tạo mới → navigate tới cùng editor với `formId = 0`, không mở overlay/popup

### 2. Color Palette — Centralized Resources

Mọi View phải dùng **shared color resources**, KHÔNG hardcode hex trực tiếp:

```xml
<!-- Định nghĩa trong UserControl.Resources -->
<SolidColorBrush x:Key="AccentBrush"      Color="#3B82F6" />   <!-- Blue-500: primary actions, links -->
<SolidColorBrush x:Key="AccentDarkBrush"   Color="#1D4ED8" />   <!-- Blue-700: hover, emphasis -->
<SolidColorBrush x:Key="AccentLightBrush"  Color="#DBEAFE" />   <!-- Blue-100: badge bg, icon bg -->
<SolidColorBrush x:Key="SurfaceBrush"      Color="#FFFFFF" />   <!-- Card, panel background -->
<SolidColorBrush x:Key="MutedBrush"        Color="#64748B" />   <!-- Slate-500: secondary text, help text -->
<SolidColorBrush x:Key="BorderBrush"       Color="#E2E8F0" />   <!-- Slate-200: card borders, separators -->
<SolidColorBrush x:Key="DangerBrush"       Color="#EF4444" />   <!-- Red-500: errors, delete actions -->
<SolidColorBrush x:Key="SuccessBrush"      Color="#22C55E" />   <!-- Green-500: success states -->
```

- Background page: `#F1F5F9` (Slate-100) → cards nổi lên trên
- Palette dựa trên **Tailwind CSS colors** → đồng nhất cross-platform

### 3. Card-based Layout

Nhóm các fields liên quan vào **card** (Border với shadow):

```xml
<Style x:Key="SectionCardStyle" TargetType="Border">
    <Setter Property="Background"      Value="{StaticResource SurfaceBrush}" />
    <Setter Property="BorderBrush"      Value="{StaticResource BorderBrush}" />
    <Setter Property="BorderThickness"  Value="1" />
    <Setter Property="CornerRadius"     Value="8" />
    <Setter Property="Padding"          Value="20,16" />
    <Setter Property="Margin"           Value="0,0,0,12" />
    <Setter Property="Effect"           Value="{StaticResource CardShadow}" />
</Style>
```

- Mỗi card có **title viết HOA** (VD: "THÔNG TIN CƠ BẢN", "MÔ TẢ", "PHIÊN BẢN")
- Shadow nhẹ: `BlurRadius="8" ShadowDepth="1" Opacity="0.08"`
- Không dùng flat layout (StackPanel trần) — luôn wrap trong card

### 4. Form Field Pattern — Label + Input + Help Text

Mỗi field theo cấu trúc 3 phần:

```xml
<StackPanel Style="{StaticResource FieldGroupStyle}">
    <!-- 1. Label -->
    <TextBlock Text="Tên Field *" Style="{StaticResource FieldLabelStyle}" />
    <!-- 2. Input control (DevExpress) -->
    <dxe:TextEdit EditValue="{Binding ...}"
                  NullText="VD: giá trị mẫu..."
                  ToolTip="Tooltip chi tiết khi hover" />
    <!-- 3. Help text — giải thích ý nghĩa field -->
    <TextBlock Style="{StaticResource HelpTextStyle}"
               Text="Giải thích ngắn gọn field này dùng để làm gì, quy tắc nhập." />
</StackPanel>
```

- **Label**: `FontSize="12"`, `FontWeight="SemiBold"`, `Foreground=MutedBrush`
- **NullText**: Luôn có ví dụ cụ thể (VD: "VD: PO_ORDER", không dùng "Nhập...")
- **ToolTip**: Mô tả chi tiết, hiện khi hover
- **Help text**: `FontSize="11"`, `Opacity="0.75"`, giải thích ý nghĩa + quy tắc
- **Trường bắt buộc**: Label kèm dấu `*`

### 5. Inline Documentation — Intro Panel

Mỗi màn hình/tab có **intro panel** ở đầu, giải thích:
- Màn hình này dùng để làm gì
- Đối tượng dữ liệu là gì
- Các trường bắt buộc

```xml
<Border Background="#F0F9FF" CornerRadius="8" Padding="16,14"
        BorderBrush="#BAE6FD" BorderThickness="1" Margin="0,0,0,16">
    <DockPanel>
        <Border DockPanel.Dock="Left" Width="36" Height="36"
                CornerRadius="18" Background="{StaticResource AccentLightBrush}"
                Margin="0,0,14,0" VerticalAlignment="Top">
            <TextBlock Text="📋" FontSize="16"
                       HorizontalAlignment="Center" VerticalAlignment="Center" />
        </Border>
        <StackPanel>
            <TextBlock Text="Tiêu đề mô tả" FontSize="14" FontWeight="SemiBold"
                       Foreground="#0C4A6E" />
            <TextBlock TextWrapping="Wrap" FontSize="12" Foreground="#0369A1" LineHeight="18">
                Nội dung mô tả chi tiết...
            </TextBlock>
        </StackPanel>
    </DockPanel>
</Border>
```

### 6. Header Bar Pattern

Header gồm: Back button → Accent bar → Title + subtitle → Action buttons

```
[←] | ▎ Tên Form                    [Lưu] [Dependencies] [Publish]
         FORM_CODE · v3 · web
```

- **Accent bar**: Border dọc `Width="3"` color `AccentBrush` → điểm nhấn visual
- **Platform badge**: nền xanh lá `#F0FDF4`, text `#16A34A`
- **Dirty indicator**: Badge vàng "Chưa lưu" (`#FEF3C7` + `#D97706`), KHÔNG dùng chấm tròn
- Toolbar **tách riêng row** khỏi header

### 7. Empty State

Khi chưa có dữ liệu hoặc chưa chọn item → hiện empty state rõ ràng:

```xml
<Border Width="64" Height="64" CornerRadius="32" Background="#F1F5F9">
    <TextBlock Text="☝" FontSize="28" Foreground="{StaticResource MutedBrush}" />
</Border>
<TextBlock Text="Hành động cần làm" FontSize="14" Foreground="{StaticResource MutedBrush}" />
<TextBlock Text="Mô tả bổ sung" FontSize="12" Opacity="0.7" />
```

### 8. Data Source — Không dùng Mock Data

- **KHÔNG hardcode mock data** trong ViewModel (VD: `LoadTableOptions()` với list cứng)
- Lookup data (ComboBox, SearchLookup) **luôn query từ DB** qua `IFormDataService`
- Nếu DB chưa cấu hình (`IsConfigured = false`) → trả list rỗng, ComboBox hiện trống
- TreeView, DataGrid data cũng phải từ DB hoặc service, không fake

### 9. Font

- UI text: System default (Segoe UI)
- Monospace values (code, checksum, ID): `Cascadia Code, Consolas, monospace`
- Không dùng font custom ngoài 2 loại trên

### 10. Quy tắc WPF cụ thể — Tránh lỗi build

- **KHÔNG dùng `TextTransform`** — WPF TextBlock không có property này (khác CSS)
- **KHÔNG dùng `TextDecorationLine`** — dùng `TextDecorations` thay thế
- **KHÔNG dùng CSS-style properties** trong XAML — luôn kiểm tra WPF API trước
- Separator trong toolbar: dùng `<Border Style="{StaticResource ToolbarSeparatorStyle}" />`
  thay vì `<Separator Style="{StaticResource {x:Static ToolBar.SeparatorStyleKey}}" />`
