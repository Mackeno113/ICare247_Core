// File    : SettingsViewModel.cs
// Module  : Shell
// Layer   : Presentation
// Purpose : ViewModel cho màn hình Cài Đặt — thiết lập connection string SQL Server.

using ConfigStudio.WPF.UI.Core.Interfaces;
using ConfigStudio.WPF.UI.Core.ViewModels;
using Microsoft.Data.SqlClient;
using Prism.Commands;

namespace ConfigStudio.WPF.UI.ViewModels;

/// <summary>
/// Quản lý cài đặt kết nối DB. Người dùng nhập từng trường (Server, Database,
/// User, Password) — ViewModel build connection string và ghi vào
/// <c>%APPDATA%\ICare247\ConfigStudio\appsettings.json</c>.
/// </summary>
public sealed class SettingsViewModel : ViewModelBase
{
    private readonly IAppConfigService _appConfig;

    // ── Connection fields ────────────────────────────────────
    private string _server = "localhost";
    public string Server
    {
        get => _server;
        set { if (SetProperty(ref _server, value)) ClearStatus(); }
    }

    private string _database = "ICare247_Config";
    public string Database
    {
        get => _database;
        set { if (SetProperty(ref _database, value)) ClearStatus(); }
    }

    private string _userId = "sa";
    public string UserId
    {
        get => _userId;
        set { if (SetProperty(ref _userId, value)) ClearStatus(); }
    }

    /// <summary>
    /// Password không dùng SetProperty vì PasswordBox không hỗ trợ binding.
    /// Code-behind cập nhật property này khi PasswordChanged.
    /// </summary>
    public string Password { get; set; } = "";

    private bool _trustServerCertificate = true;
    public bool TrustServerCertificate
    {
        get => _trustServerCertificate;
        set { if (SetProperty(ref _trustServerCertificate, value)) ClearStatus(); }
    }

    // ── Tenant ──────────────────────────────────────────────
    private int _tenantId = 1;
    public int TenantId
    {
        get => _tenantId;
        set => SetProperty(ref _tenantId, value);
    }

    // ── Status ───────────────────────────────────────────────
    private string _statusMessage = "";
    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    private bool _isStatusSuccess;
    public bool IsStatusSuccess
    {
        get => _isStatusSuccess;
        private set => SetProperty(ref _isStatusSuccess, value);
    }

    private bool _isStatusError;
    public bool IsStatusError
    {
        get => _isStatusError;
        private set => SetProperty(ref _isStatusError, value);
    }

    private bool _isStatusVisible;
    public bool IsStatusVisible
    {
        get => _isStatusVisible;
        private set => SetProperty(ref _isStatusVisible, value);
    }

    private bool _isBusy;
    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value))
            {
                TestConnectionCommand.RaiseCanExecuteChanged();
                SaveCommand.RaiseCanExecuteChanged();
            }
        }
    }

    // ── Info ─────────────────────────────────────────────────
    public string ConfigFilePath => _appConfig.ConfigFilePath;

    // ── Commands ─────────────────────────────────────────────
    public DelegateCommand TestConnectionCommand { get; }
    public DelegateCommand SaveCommand { get; }

    public SettingsViewModel(IAppConfigService appConfig)
    {
        _appConfig = appConfig;

        TestConnectionCommand = new DelegateCommand(
            async () => await ExecuteTestAsync(), () => !IsBusy);

        SaveCommand = new DelegateCommand(
            async () => await ExecuteSaveAsync(), () => !IsBusy);

        LoadFromConfig();
    }

    // ── Helpers ──────────────────────────────────────────────

    /// <summary>
    /// Parse connection string hiện tại thành các trường riêng lẻ.
    /// Dùng SqlConnectionStringBuilder để parse an toàn.
    /// </summary>
    private void LoadFromConfig()
    {
        TenantId = _appConfig.TenantId;

        if (!_appConfig.IsConfigured
         || string.IsNullOrWhiteSpace(_appConfig.ConnectionString))
            return;

        try
        {
            var builder = new SqlConnectionStringBuilder(_appConfig.ConnectionString);
            Server                 = builder.DataSource;
            Database               = builder.InitialCatalog;
            UserId                 = builder.UserID;
            Password               = builder.Password;
            TrustServerCertificate = builder.TrustServerCertificate;
        }
        catch
        {
            // NOTE: Connection string format lạ → giữ giá trị mặc định
        }
    }

    /// <summary>Build connection string từ các trường riêng lẻ.</summary>
    private string BuildConnectionString()
    {
        var builder = new SqlConnectionStringBuilder
        {
            DataSource             = Server,
            InitialCatalog         = Database,
            UserID                 = UserId,
            Password               = Password,
            TrustServerCertificate = TrustServerCertificate,
        };
        return builder.ConnectionString;
    }

    private async Task ExecuteTestAsync()
    {
        IsBusy = true;
        SetStatus(success: false, "Đang kiểm tra kết nối...", visible: true, isError: false);

        try
        {
            var error = await _appConfig.TestConnectionAsync(BuildConnectionString());
            if (error is null)
                SetStatus(success: true, "✓  Kết nối thành công!", visible: true, isError: false);
            else
                SetStatus(success: false, error, visible: true, isError: true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ExecuteSaveAsync()
    {
        // ── 1. Test trước khi lưu ────────────────────────────
        IsBusy = true;
        SetStatus(success: false, "Đang kiểm tra kết nối...", visible: true, isError: false);

        try
        {
            var error = await _appConfig.TestConnectionAsync(BuildConnectionString());
            if (error is not null)
            {
                SetStatus(success: false, $"Lỗi kết nối — chưa lưu:\n{error}", visible: true, isError: true);
                return;
            }

            // ── 2. Ghi vào file ──────────────────────────────
            SetStatus(success: false, "Đang lưu...", visible: true, isError: false);
            await _appConfig.SaveAsync(BuildConnectionString(), TenantId);
            SetStatus(success: true, "✓  Đã lưu cài đặt thành công!", visible: true, isError: false);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void SetStatus(bool success, string message, bool visible, bool isError)
    {
        StatusMessage    = message;
        IsStatusSuccess  = success;
        IsStatusError    = isError;
        IsStatusVisible  = visible;
    }

    private void ClearStatus() => SetStatus(false, "", false, false);
}
