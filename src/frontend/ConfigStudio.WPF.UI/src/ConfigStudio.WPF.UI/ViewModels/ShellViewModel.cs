// File    : ShellViewModel.cs
// Module  : Shell
// Layer   : Presentation
// Purpose : Quan ly navigation, trang thai shell, command cua title bar va doi theme.

using System.Collections.ObjectModel;
using System.Reflection;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using ConfigStudio.WPF.UI.Core.Constants;
using ConfigStudio.WPF.UI.Core.Interfaces;
using ConfigStudio.WPF.UI.Core.Services;
using ConfigStudio.WPF.UI.Core.ViewModels;
using ConfigStudio.WPF.UI.Models;
using ConfigStudio.WPF.UI.Services;
using Prism.Commands;
using Prism.Navigation;
using Prism.Navigation.Regions;

namespace ConfigStudio.WPF.UI.ViewModels;

public class ShellViewModel : ViewModelBase
{
    private readonly IRegionManager? _regionManager;
    private readonly IThemeService? _themeService;
    private readonly INavigationHistoryService? _history;
    private readonly IUserNotifier? _notifier;
    private NavigationItem? _selectedItem;
    private bool _isSidebarCollapsed;
    private string _currentThemeDisplayName = "Light Ocean";

    public ShellViewModel(
        IRegionManager? regionManager = null,
        IThemeService? themeService = null,
        INavigationHistoryService? history = null,
        IUserNotifier? notifier = null)
    {
        _regionManager = regionManager;
        _themeService = themeService;
        _history = history;
        _notifier = notifier;
        if (_history is not null) _history.Changed += OnHistoryChanged;

        // Banner thông báo dùng chung — subscribe để hiện lỗi/cảnh báo cho user.
        DismissNotificationCommand = new DelegateCommand(HideNotification);
        _notificationTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(8) };
        _notificationTimer.Tick += (_, _) => HideNotification();
        if (_notifier is not null) _notifier.Raised += OnNotificationRaised;

        NavigationItems = [];

        TenantName = "DEMO";
        CurrentUser = "admin";
        ConnectionStatus = "Connected";
        CacheStatus = "Redis OK";
        AppVersion = GetAppVersion();

        NavigateCommand = new DelegateCommand<NavigationItem?>(Navigate);
        NavigateByViewNameCommand = new DelegateCommand<string?>(NavigateByViewName);
        ToggleSidebarCommand = new DelegateCommand(ToggleSidebar);
        GoBackCommand    = new DelegateCommand(() => _history?.GoBack(),    () => _history?.CanGoBack    == true);
        GoForwardCommand = new DelegateCommand(() => _history?.GoForward(), () => _history?.CanGoForward == true);
        JumpToCrumbCommand = new DelegateCommand<NavigationCrumb?>(c => { if (c is not null) _history?.JumpToCrumb(c); });
        WindowMinimizeCommand = new DelegateCommand<Window?>(MinimizeWindow);
        WindowMaximizeCommand = new DelegateCommand<Window?>(MaximizeWindow);
        WindowCloseCommand = new DelegateCommand<Window?>(CloseWindow);
        ChangeThemeCommand = new DelegateCommand<string?>(ChangeTheme);

        InitNavigationItems();
        BuildSidebarEntries();
        InitThemeDisplay();
        Navigate(NavigationItems.FirstOrDefault(i => i.NavigateTo == ViewNames.Dashboard));
    }

    public ObservableCollection<NavigationItem> NavigationItems { get; }

    public List<SidebarEntry> SidebarEntries { get; } = [];

    public NavigationItem? SelectedItem
    {
        get => _selectedItem;
        set => SetProperty(ref _selectedItem, value);
    }

    public bool IsSidebarCollapsed
    {
        get => _isSidebarCollapsed;
        set => SetProperty(ref _isSidebarCollapsed, value);
    }

    public string CurrentThemeDisplayName
    {
        get => _currentThemeDisplayName;
        private set => SetProperty(ref _currentThemeDisplayName, value);
    }

    public string TenantName { get; }

    public string CurrentUser { get; }

    public string ConnectionStatus { get; }

    public string CacheStatus { get; }

    public string AppVersion { get; }

    public DelegateCommand<NavigationItem?> NavigateCommand { get; }

    // Navigate bang ViewName (vd: "Dashboard", "FormManager") — dung cho keyboard shortcut.
    public DelegateCommand<string?> NavigateByViewNameCommand { get; }

    public DelegateCommand ToggleSidebarCommand { get; }

    public DelegateCommand<Window?> WindowMinimizeCommand { get; }

    public DelegateCommand<Window?> WindowMaximizeCommand { get; }

    public DelegateCommand<Window?> WindowCloseCommand { get; }

    public DelegateCommand<string?> ChangeThemeCommand { get; }

    public DelegateCommand GoBackCommand    { get; }
    public DelegateCommand GoForwardCommand { get; }
    public DelegateCommand<NavigationCrumb?> JumpToCrumbCommand { get; }

    public IReadOnlyList<NavigationCrumb> Breadcrumbs => _history?.Crumbs ?? [];
    public bool CanGoBack    => _history?.CanGoBack    == true;
    public bool CanGoForward => _history?.CanGoForward == true;
    public bool HasBreadcrumbs => Breadcrumbs.Count > 0;

    private void OnHistoryChanged(object? sender, System.EventArgs e)
    {
        RaisePropertyChanged(nameof(Breadcrumbs));
        RaisePropertyChanged(nameof(CanGoBack));
        RaisePropertyChanged(nameof(CanGoForward));
        RaisePropertyChanged(nameof(HasBreadcrumbs));
        GoBackCommand.RaiseCanExecuteChanged();
        GoForwardCommand.RaiseCanExecuteChanged();
    }

    // ── Banner thông báo dùng chung (error surface toàn cục) ──────────────
    private readonly DispatcherTimer _notificationTimer;

    private bool _isNotificationVisible;
    /// <summary>True khi đang hiện banner thông báo — bind Visibility ở shell.</summary>
    public bool IsNotificationVisible
    {
        get => _isNotificationVisible;
        private set => SetProperty(ref _isNotificationVisible, value);
    }

    private string _notificationMessage = "";
    /// <summary>Nội dung chính của thông báo.</summary>
    public string NotificationMessage
    {
        get => _notificationMessage;
        private set => SetProperty(ref _notificationMessage, value);
    }

    private string _notificationDetail = "";
    /// <summary>Chi tiết kỹ thuật (message lỗi gốc) — hiện phụ, hỗ trợ debug.</summary>
    public string NotificationDetail
    {
        get => _notificationDetail;
        private set { if (SetProperty(ref _notificationDetail, value)) RaisePropertyChanged(nameof(HasNotificationDetail)); }
    }

    /// <summary>True khi có chi tiết kỹ thuật để hiện dòng phụ.</summary>
    public bool HasNotificationDetail => !string.IsNullOrEmpty(_notificationDetail);

    private Brush _notificationBrush = Brushes.SteelBlue;
    /// <summary>Màu banner theo mức độ (đỏ/cam/xanh lá/xanh dương).</summary>
    public Brush NotificationBrush
    {
        get => _notificationBrush;
        private set => SetProperty(ref _notificationBrush, value);
    }

    private string _notificationIcon = "ℹ";
    /// <summary>Icon theo mức độ.</summary>
    public string NotificationIcon
    {
        get => _notificationIcon;
        private set => SetProperty(ref _notificationIcon, value);
    }

    /// <summary>Đóng banner thủ công.</summary>
    public DelegateCommand DismissNotificationCommand { get; }

    /// <summary>
    /// Xử lý thông báo mới từ IUserNotifier: nạp nội dung + màu theo mức độ, hiện banner,
    /// khởi động lại timer auto-ẩn. Lỗi (Error) giữ lâu hơn để user kịp đọc.
    /// </summary>
    private void OnNotificationRaised(UserNotification n)
    {
        NotificationMessage = n.Message;
        NotificationDetail  = n.Detail ?? "";
        (NotificationBrush, NotificationIcon) = n.Severity switch
        {
            NotificationSeverity.Error   => ((Brush)new SolidColorBrush(Color.FromRgb(0xDC, 0x26, 0x26)), "⛔"),
            NotificationSeverity.Warning => (new SolidColorBrush(Color.FromRgb(0xD9, 0x77, 0x06)), "⚠"),
            NotificationSeverity.Success => (new SolidColorBrush(Color.FromRgb(0x16, 0xA3, 0x4A)), "✓"),
            _                            => (new SolidColorBrush(Color.FromRgb(0x25, 0x63, 0xEB)), "ℹ"),
        };
        IsNotificationVisible = true;

        _notificationTimer.Stop();
        _notificationTimer.Interval = TimeSpan.FromSeconds(n.Severity == NotificationSeverity.Error ? 15 : 6);
        _notificationTimer.Start();
    }

    private void HideNotification()
    {
        _notificationTimer.Stop();
        IsNotificationVisible = false;
    }

    private void InitNavigationItems()
    {
        NavigationItems.Clear();

        NavigationItems.Add(new NavigationItem
        {
            Title = "Dashboard",
            Icon = "⌂",
            NavigateTo = ViewNames.Dashboard,
            Level = 0
        });

        NavigationItems.Add(new NavigationItem
        {
            Title = "Forms",
            Icon = "📄",
            Level = 0,
            IsExpanded = true,
            Children =
            [
                
                new NavigationItem
                {
                    Title = "Sys Table",
                    Icon = "⌗",
                    NavigateTo = ViewNames.SysTableManager,
                    Level = 1
                },
                new NavigationItem
                {
                    Title = "Sys Lookup",
                    Icon = "📋",
                    NavigateTo = ViewNames.SysLookupManager,
                    Level = 1
                },
                new NavigationItem
                {
                    Title = LookupTemplateUiText.ScreenTitle,
                    Icon = "⌕",
                    NavigateTo = ViewNames.LookupTemplateManager,
                    Level = 1
                },
                new NavigationItem
                {
                    Title = "Form List",
                    Icon = "≡",
                    NavigateTo = ViewNames.FormManager,
                    Level = 1
                },
                new NavigationItem
                {
                    Title = "Views (Grid/Tree)",
                    Icon = "▦",
                    NavigateTo = ViewNames.ViewManager,
                    Level = 1
                },
                new NavigationItem
                {
                    Title = "Quan hệ (Relation)",
                    Icon = "🔗",
                    NavigateTo = ViewNames.RelationManager,
                    Level = 1
                },
                new NavigationItem
                {
                    Title = "New Form",
                    Icon = "+",
                    NavigateTo = ViewNames.FormEditor,
                    Level = 1,
                    Parameters = new NavigationParameters
                    {
                        { "mode", "new" }
                    }
                }
            ]
        });

        NavigationItems.Add(new NavigationItem
        {
            Title = "Validation Rules",
            Icon = "✓",
            NavigateTo = ViewNames.ValidationRuleEditor,
            Level = 0
        });

        NavigationItems.Add(new NavigationItem
        {
            Title = "Events",
            Icon = "⚡",
            NavigateTo = ViewNames.EventEditor,
            Level = 0
        });

        NavigationItems.Add(new NavigationItem
        {
            Title = "Grammar",
            Icon = "f()",
            Level = 0,
            IsExpanded = true,
            Children =
            [
                new NavigationItem
                {
                    Title = "Functions",
                    Icon = "f()",
                    NavigateTo = ViewNames.GrammarLibrary,
                    Level = 1
                },
                new NavigationItem
                {
                    Title = "Operators",
                    Icon = "±",
                    NavigateTo = ViewNames.GrammarLibrary,
                    Level = 1,
                    Parameters = new NavigationParameters
                    {
                        { "tab", "operators" }
                    }
                }
            ]
        });

        NavigationItems.Add(new NavigationItem
        {
            Title = "i18n Keys",
            Icon = "🌐",
            NavigateTo = ViewNames.I18nManager,
            Level = 0
        });

        NavigationItems.Add(new NavigationItem
        {
            Title = "Mẫu tài liệu",
            Icon = "📄",
            NavigateTo = ViewNames.DocTemplateEditor,
            Level = 0
        });

        NavigationItems.Add(new NavigationItem
        {
            IsDivider = true
        });

        NavigationItems.Add(new NavigationItem
        {
            Title = "Settings",
            Icon = "⚙",
            NavigateTo = ViewNames.Settings,
            Level = 0
        });
    }

    private void BuildSidebarEntries()
    {
        SidebarEntries.Clear();
        foreach (var item in NavigationItems)
        {
            if (item.IsDivider)
            {
                SidebarEntries.Add(new SidebarEntry { IsDivider = true });
                continue;
            }
            if (item.Children.Count > 0)
            {
                SidebarEntries.Add(new SidebarEntry { IsHeader = true, Title = item.Title, Icon = item.Icon });
                foreach (var child in item.Children)
                    SidebarEntries.Add(new SidebarEntry { Title = child.Title, Icon = child.Icon, NavItem = child });
            }
            else
            {
                SidebarEntries.Add(new SidebarEntry { Title = item.Title, Icon = item.Icon, NavItem = item });
            }
        }
    }

    // Tim NavigationItem theo ViewName (depth-first qua ca Children) va navigate toi.
    private void NavigateByViewName(string? viewName)
    {
        if (string.IsNullOrWhiteSpace(viewName)) return;
        var target = Flatten(NavigationItems)
            .FirstOrDefault(n => string.Equals(n.NavigateTo, viewName, StringComparison.Ordinal));
        if (target is not null) Navigate(target);
    }

    private void Navigate(NavigationItem? item)
    {
        if (item is null || item.IsDivider)
        {
            return;
        }

        if (item.Children.Count > 0 && string.IsNullOrWhiteSpace(item.NavigateTo))
        {
            item.IsExpanded = !item.IsExpanded;
            SelectItem(item);
            return;
        }

        SelectItem(item);

        if (string.IsNullOrWhiteSpace(item.NavigateTo) || _regionManager is null)
        {
            return;
        }

        if (item.Parameters is null)
        {
            _regionManager.RequestNavigate(RegionNames.Content, item.NavigateTo);
            return;
        }

        _regionManager.RequestNavigate(RegionNames.Content, item.NavigateTo, item.Parameters);
    }

    private void SelectItem(NavigationItem item)
    {
        foreach (var navItem in Flatten(NavigationItems))
        {
            navItem.IsSelected = ReferenceEquals(navItem, item);
        }

        SelectedItem = item;
    }

    private static IEnumerable<NavigationItem> Flatten(IEnumerable<NavigationItem> items)
    {
        foreach (var item in items)
        {
            yield return item;

            foreach (var child in Flatten(item.Children))
            {
                yield return child;
            }
        }
    }

    private void ToggleSidebar()
    {
        IsSidebarCollapsed = !IsSidebarCollapsed;
    }

    private void ChangeTheme(string? themeCode)
    {
        if (_themeService is null)
        {
            return;
        }

        var theme = string.Equals(themeCode, "slate", StringComparison.OrdinalIgnoreCase)
            ? AppTheme.SlateProfessional
            : AppTheme.LightOcean;

        _themeService.ApplyTheme(theme);
        CurrentThemeDisplayName = GetThemeDisplayName(theme);
    }

    private void InitThemeDisplay()
    {
        if (_themeService is null)
        {
            CurrentThemeDisplayName = GetThemeDisplayName(AppTheme.LightOcean);
            return;
        }

        CurrentThemeDisplayName = GetThemeDisplayName(_themeService.CurrentTheme);
    }

    private static string GetThemeDisplayName(AppTheme theme)
    {
        return theme == AppTheme.SlateProfessional
            ? "Slate Professional"
            : "Light Ocean";
    }

    private static void MinimizeWindow(Window? window)
    {
        if (window is null)
        {
            return;
        }

        window.WindowState = WindowState.Minimized;
    }

    private static void MaximizeWindow(Window? window)
    {
        if (window is null)
        {
            return;
        }

        window.WindowState = window.WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;
    }

    private static void CloseWindow(Window? window)
    {
        window?.Close();
    }

    private static string GetAppVersion()
    {
        var version = Assembly.GetEntryAssembly()?.GetName().Version;
        return version is null ? "1.0.0" : $"v{version.Major}.{version.Minor}.{version.Build}";
    }
}
