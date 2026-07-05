// File    : App.xaml.cs
// Module  : Bootstrap
// Layer   : Presentation
// Purpose : Khoi tao Prism application, shell va module catalog.

using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using DevExpress.Xpf.Core;
using ConfigStudio.WPF.UI.Core.Constants;
using ConfigStudio.WPF.UI.Core.Interfaces;
using ConfigStudio.WPF.UI.Core.Services;
using ConfigStudio.WPF.UI.Infrastructure;
using ConfigStudio.WPF.UI.Infrastructure.Logging;
using ConfigStudio.WPF.UI.Modules.Events;
using ConfigStudio.WPF.UI.Modules.Forms;
using ConfigStudio.WPF.UI.Modules.Grammar;
using ConfigStudio.WPF.UI.Modules.I18n;
using ConfigStudio.WPF.UI.Modules.Rules;
using ConfigStudio.WPF.UI.Services;
using ConfigStudio.WPF.UI.ViewModels;
using ConfigStudio.WPF.UI.Views;
using Prism.Ioc;
using Prism.Modularity;
using Prism.Mvvm;
using Prism.Unity;

namespace ConfigStudio.WPF.UI;

public partial class App : PrismApplication
{
    private IAppLogger? _logger;
    private IUserNotifier? _notifier;

    public App()
    {
        // Đặt theme DevExpress TRƯỚC InitializeComponent
        ApplicationThemeHelper.ApplicationThemeName = Theme.Office2019ColorfulName;
    }

    protected override void ConfigureViewModelLocator()
    {
        base.ConfigureViewModelLocator();
        ViewModelLocationProvider.Register<MainWindow, MainWindowViewModel>();
    }

    protected override Window CreateShell()
        => Container.Resolve<MainWindow>();

    protected override void RegisterTypes(IContainerRegistry containerRegistry)
    {
        // Logger singleton — tách lỗi SQL vs C# ra 2 file (xem Infrastructure\Logging)
        containerRegistry.RegisterSingleton<IAppLogger, SerilogAppLogger>();
        // Notifier singleton — "nơi báo lỗi" cho người dùng thấy trên shell (banner).
        containerRegistry.RegisterSingleton<IUserNotifier, UserNotifier>();

        containerRegistry.RegisterSingleton<IThemeService, ThemeService>();
        containerRegistry.RegisterSingleton<INavigationHistoryService, NavigationHistoryService>();

        // ── Infrastructure: DB config + data services ────────
        // Singleton vì AppConfigService giữ trạng thái IsConfigured sau LoadAsync()
        containerRegistry.RegisterSingleton<IAppConfigService, AppConfigService>();
        containerRegistry.Register<IFormDataService, FormDataService>();
        containerRegistry.Register<IFormDetailDataService, FormDetailDataService>();
        containerRegistry.Register<IFieldDataService, FieldDataService>();
        containerRegistry.Register<IRuleDataService, RuleDataService>();
        containerRegistry.Register<IEventDataService, EventDataService>();
        containerRegistry.Register<IGrammarDataService, GrammarDataService>();
        // Singleton — giữ cache i18n (toàn bộ Sys_Resource) sống xuyên suốt phiên,
        // tránh N+1 query khi mở form (mỗi section/field resolve 1 round-trip).
        containerRegistry.RegisterSingleton<II18nDataService, I18nDataService>();
        containerRegistry.Register<ISysLookupDataService, SysLookupDataService>();
        containerRegistry.Register<IViewDataService, ViewDataService>();
        containerRegistry.Register<IRelationDataService, RelationDataService>();
        containerRegistry.Register<IPublishCheckService, PublishCheckService>();
        containerRegistry.Register<IImpactPreviewService, ImpactPreviewService>();
        containerRegistry.Register<ISchemaInspectorService, SchemaInspectorService>();
        containerRegistry.Register<ISchemaMaintenanceService, SchemaMaintenanceService>();

        containerRegistry.RegisterForNavigation<DashboardView, DashboardViewModel>(ViewNames.Dashboard);
        containerRegistry.RegisterForNavigation<SettingsView, SettingsViewModel>(ViewNames.Settings);
    }

    protected override void ConfigureModuleCatalog(IModuleCatalog moduleCatalog)
    {
        moduleCatalog.AddModule<FormsModule>();
        moduleCatalog.AddModule<RulesModule>();
        moduleCatalog.AddModule<EventsModule>();
        moduleCatalog.AddModule<GrammarModule>();
        moduleCatalog.AddModule<I18nModule>();
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();

        // Resolve logger + notifier sớm + gắn lưới an toàn bắt lỗi chưa xử lý toàn app.
        _logger = Container.Resolve<IAppLogger>();
        _notifier = Container.Resolve<IUserNotifier>();
        WireGlobalExceptionHandlers();
        _logger.Info("ConfigStudio khởi động.");

        // NOTE: Preload cấu hình DB ngay lúc startup để các màn hình đầu tiên
        // có thể dùng ConnectionString/Tenant_Id mà không cần mở Settings trước.
        var appConfig = Container.Resolve<IAppConfigService>();
        _ = PreloadAppConfigAsync(appConfig);
    }

    /// <summary>
    /// Gắn 3 handler bắt mọi exception chưa được catch — lưới an toàn cuối cùng.
    /// Logger tự phân loại SQL vs C# nên không cần biết nguồn lỗi ở đây.
    /// </summary>
    private void WireGlobalExceptionHandlers()
    {
        // Lỗi trên UI thread (DispatcherUnhandledException)
        DispatcherUnhandledException += (_, e) =>
        {
            _logger?.Capture(e.Exception, "DispatcherUnhandledException");
            _notifier?.NotifyError("Đã xảy ra lỗi không mong muốn.", e.Exception);
            e.Handled = true; // tránh crash app — lỗi đã được ghi
        };

        // Exception trong Task async không được await/observe
        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            _logger?.Capture(e.Exception, "UnobservedTaskException");
            e.SetObserved();
        };

        // Lỗi ở thread non-UI (thường không cứu được, nhưng phải ghi lại)
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            if (e.ExceptionObject is Exception ex)
                _logger?.Capture(ex, $"UnhandledException (terminating={e.IsTerminating})");
            _logger?.Flush();
        };
    }

    /// <summary>Đẩy log xuống đĩa khi app thoát.</summary>
    protected override void OnExit(ExitEventArgs e)
    {
        _logger?.Info("ConfigStudio thoát.");
        _logger?.Flush();
        base.OnExit(e);
    }

    /// <summary>
    /// Nạp appsettings từ %APPDATA% khi app khởi động.
    /// Nếu lỗi thì ghi log, không làm crash ứng dụng.
    /// </summary>
    private async Task PreloadAppConfigAsync(IAppConfigService appConfig)
    {
        try
        {
            await appConfig.LoadAsync();
        }
        catch (Exception ex)
        {
            _logger?.Capture(ex, "AppConfig preload failed");
        }
    }
}
