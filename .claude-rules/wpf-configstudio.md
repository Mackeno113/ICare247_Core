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
