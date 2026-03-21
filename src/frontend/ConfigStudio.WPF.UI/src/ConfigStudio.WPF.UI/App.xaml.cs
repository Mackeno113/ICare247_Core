// File    : App.xaml.cs
// Module  : Bootstrap
// Layer   : Presentation
// Purpose : Khoi tao Prism application, shell va module catalog.

using System.Windows;
using System.IO;
using DevExpress.Xpf.Core;
using ConfigStudio.WPF.UI.Core.Constants;
using ConfigStudio.WPF.UI.Core.Interfaces;
using ConfigStudio.WPF.UI.Core.Services;
using ConfigStudio.WPF.UI.Infrastructure;
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
    private static readonly string StartupLogPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "ICare247", "ConfigStudio", "logs", "startup.log");

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
        containerRegistry.RegisterSingleton<IThemeService, ThemeService>();

        // ── Infrastructure: DB config + data services ────────
        // Singleton vì AppConfigService giữ trạng thái IsConfigured sau LoadAsync()
        containerRegistry.RegisterSingleton<IAppConfigService, AppConfigService>();
        containerRegistry.Register<IFormDataService, FormDataService>();
        containerRegistry.Register<IFormDetailDataService, FormDetailDataService>();
        containerRegistry.Register<IFieldDataService, FieldDataService>();
        containerRegistry.Register<IRuleDataService, RuleDataService>();
        containerRegistry.Register<IEventDataService, EventDataService>();
        containerRegistry.Register<IGrammarDataService, GrammarDataService>();
        containerRegistry.Register<II18nDataService, I18nDataService>();
        containerRegistry.Register<IPublishCheckService, PublishCheckService>();
        containerRegistry.Register<IImpactPreviewService, ImpactPreviewService>();
        containerRegistry.Register<ISchemaInspectorService, SchemaInspectorService>();

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

        // NOTE: Preload cấu hình DB ngay lúc startup để các màn hình đầu tiên
        // có thể dùng ConnectionString/Tenant_Id mà không cần mở Settings trước.
        var appConfig = Container.Resolve<IAppConfigService>();
        _ = PreloadAppConfigAsync(appConfig);
    }

    /// <summary>
    /// Nạp appsettings từ %APPDATA% khi app khởi động.
    /// Nếu lỗi thì ghi log local, không làm crash ứng dụng.
    /// </summary>
    private static async Task PreloadAppConfigAsync(IAppConfigService appConfig)
    {
        try
        {
            await appConfig.LoadAsync();
        }
        catch (Exception ex)
        {
            try
            {
                var dir = Path.GetDirectoryName(StartupLogPath);
                if (!string.IsNullOrWhiteSpace(dir))
                    Directory.CreateDirectory(dir);

                var log = $"""
                    [{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] AppConfig preload failed
                    Message: {ex.Message}
                    StackTrace:
                    {ex.StackTrace}
                    ----------------------------------------
                    """;

                await File.AppendAllTextAsync(StartupLogPath, log + Environment.NewLine);
            }
            catch
            {
                // NOTE: Không để lỗi ghi log ảnh hưởng luồng startup.
            }
        }
    }
}
