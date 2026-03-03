# PROMPT 01 — Shell + Navigation

> **Dán prompt này vào Claude Code để implement màn hình Shell.**
> Đảm bảo CLAUDE.md đã có trong project root trước khi chạy.

---

## PROMPT

```
Đọc file CLAUDE.md trong project root để nắm toàn bộ convention trước khi code.

Tôi cần implement Shell (MainWindow) cho ICare247.ConfigStudio.
Tạo đầy đủ các files sau trong project ICare247.ConfigStudio:

─── FILES CẦN TẠO ───────────────────────────────────────

1. Views/MainWindow.xaml + MainWindow.xaml.cs
2. ViewModels/ShellViewModel.cs
3. Models/NavigationItem.cs
4. Converters/LevelToIndentConverter.cs   ← dùng cho sub-items TreeView
5. Themes/Shell.xaml                      ← style cho titlebar, sidebar
6. Themes/Navigation.xaml                 ← style cho NavigationItem trong TreeView

─── LAYOUT YÊU CẦU ─────────────────────────────────────

MainWindow layout (DockPanel):
┌─ TitleBar (DockPanel.Dock=Top, Height=48, Background=#1A237E) ────────────────┐
│  [☰ toggle sidebar]  "ICare247 Config Studio"  [Tenant badge] [User] [─][□][✕]│
└───────────────────────────────────────────────────────────────────────────────┘
┌─ StatusBar (DockPanel.Dock=Bottom, Height=28, Background=#0D1757) ─────────────┐
│  DB: Connected  │  Cache: Redis OK  │  Tenant: DEMO  │  v1.0.0          Ready  │
└───────────────────────────────────────────────────────────────────────────────┘
┌─ Sidebar (DockPanel.Dock=Left, Width=240, collapsible→48) ──────────────────────┐
│  TreeView với NavigationItems                                                   │
└────────────────────────────────────────────────────────────────────────────────┘
┌─ Content (Fill) ────────────────────────────────────────────────────────────────┐
│  ContentControl với prism:RegionManager.RegionName="ContentRegion"              │
└────────────────────────────────────────────────────────────────────────────────┘

─── NAVIGATION ITEMS ─────────────────────────────────────

NavigationItem model:
  - Title: string
  - Icon: PackIconKind (MaterialDesign)
  - NavigateTo: string?          ← ViewNames constant, null nếu là group header
  - Children: ObservableCollection<NavigationItem>
  - IsExpanded: bool
  - IsSelected: bool
  - Level: int                   ← 0=root, 1=sub-item (để indent)

Items cần tạo trong ShellViewModel.InitNavigationItems():
  [0] Dashboard           → icon=Home,         navigate=ViewNames.Dashboard
  [1] Forms               → icon=Description,  navigate=null (group)
       ├ Form List        → icon=List,          navigate=ViewNames.FormManager
       └ New Form         → icon=AddBox,        navigate=ViewNames.FormEditor (mode=new)
  [2] Validation Rules    → icon=CheckCircle,   navigate=ViewNames.ValidationRuleEditor
  [3] Events              → icon=FlashOn,       navigate=ViewNames.EventEditor
  [4] Grammar             → icon=Functions,     navigate=null (group)
       ├ Functions        → icon=Code,          navigate=ViewNames.GrammarLibrary
       └ Operators        → icon=Calculate,     navigate=ViewNames.GrammarLibrary (tab=operators)
  [5] i18n Keys           → icon=Language,      navigate=ViewNames.I18nManager
  [─] divider
  [6] Settings            → icon=Settings,      navigate=ViewNames.Settings

─── SHELLVIEWMODEL PROPERTIES & COMMANDS ────────────────

Properties:
  - NavigationItems: ObservableCollection<NavigationItem>
  - SelectedItem: NavigationItem?
  - IsSidebarCollapsed: bool          ← toggle sidebar width 240↔48
  - TenantName: string                ← "DEMO" (từ config/session)
  - CurrentUser: string               ← "admin"
  - ConnectionStatus: string          ← "Connected" / "Disconnected"
  - CacheStatus: string               ← "Redis OK" / "Memory Only"
  - AppVersion: string                ← Assembly version

Commands:
  - NavigateCommand: DelegateCommand<NavigationItem>
      → if item.NavigateTo != null: _regionManager.RequestNavigate(RegionNames.Content, item.NavigateTo)
      → if item.Children.Any(): toggle item.IsExpanded
  - ToggleSidebarCommand: DelegateCommand
  - WindowMinimizeCommand, WindowMaximizeCommand, WindowCloseCommand: DelegateCommand

─── SIDEBAR STYLE YÊU CẦU ───────────────────────────────

- Background: #1A237E (xanh đậm)
- Item bình thường: text trắng, icon trắng, height=44
- Item được chọn (IsSelected=true): Background=#1565C0, có left border 3px #42A5F5
- Item hover: Background=#283593
- Sub-item (Level=1): indent thêm 16px, font nhỏ hơn (13px)
- Khi sidebar collapsed (IsSidebarCollapsed=true):
    - Width=48, chỉ hiện icon, ẩn text
    - Dùng Tooltip để hiện tên khi hover
- Transition animation cho collapse: DoubleAnimation Width 240↔48, Duration 200ms

─── CONSTRAINTS ─────────────────────────────────────────

- Dùng prism:ViewModelLocator.AutoWireViewModel="True" (không set DataContext trong code)
- TitleBar dùng WindowStyle=None, AllowsTransparency=False
- Drag window: MouseDown trên TitleBar gọi DragMove()
- Xử lý Maximize: nếu WindowState=Maximized thì border radius=0
- Tất cả colors dùng SolidColorBrush từ MaterialDesign palette
- Không hardcode màu trực tiếp trong XAML — khai báo trong Shell.xaml resource

─── OUTPUT MONG ĐỢI ─────────────────────────────────────

Sau khi chạy app:
✅ Shell hiện ra với sidebar, titlebar, statusbar
✅ Click item trên sidebar → navigate vào ContentRegion
✅ Click toggle → sidebar collapse/expand có animation
✅ Kéo titlebar để move window
✅ Nút minimize/maximize/close hoạt động
```