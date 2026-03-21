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

    // ── Target DB Connection fields ───────────────────────────
    private string _targetServer = "localhost";
    public string TargetServer
    {
        get => _targetServer;
        set { if (SetProperty(ref _targetServer, value)) ClearTargetStatus(); }
    }

    private string _targetDatabase = "";
    public string TargetDatabase
    {
        get => _targetDatabase;
        set { if (SetProperty(ref _targetDatabase, value)) ClearTargetStatus(); }
    }

    private string _targetUserId = "sa";
    public string TargetUserId
    {
        get => _targetUserId;
        set { if (SetProperty(ref _targetUserId, value)) ClearTargetStatus(); }
    }

    /// <summary>Password của Target DB — cập nhật từ code-behind (PasswordBox).</summary>
    public string TargetPassword { get; set; } = "";

    private bool _targetTrustServerCertificate = true;
    public bool TargetTrustServerCertificate
    {
        get => _targetTrustServerCertificate;
        set { if (SetProperty(ref _targetTrustServerCertificate, value)) ClearTargetStatus(); }
    }

    // ── Target Status ─────────────────────────────────────────
    private string _targetStatusMessage = "";
    public string TargetStatusMessage
    {
        get => _targetStatusMessage;
        private set => SetProperty(ref _targetStatusMessage, value);
    }

    private bool _isTargetStatusSuccess;
    public bool IsTargetStatusSuccess
    {
        get => _isTargetStatusSuccess;
        private set => SetProperty(ref _isTargetStatusSuccess, value);
    }

    private bool _isTargetStatusError;
    public bool IsTargetStatusError
    {
        get => _isTargetStatusError;
        private set => SetProperty(ref _isTargetStatusError, value);
    }

    private bool _isTargetStatusVisible;
    public bool IsTargetStatusVisible
    {
        get => _isTargetStatusVisible;
        private set => SetProperty(ref _isTargetStatusVisible, value);
    }

    // ── Info ─────────────────────────────────────────────────
    public string ConfigFilePath => _appConfig.ConfigFilePath;

    // ── Commands ─────────────────────────────────────────────
    public DelegateCommand TestConnectionCommand { get; }
    public DelegateCommand TestTargetConnectionCommand { get; }
    public DelegateCommand SaveCommand { get; }

    public SettingsViewModel(IAppConfigService appConfig)
    {
        _appConfig = appConfig;

        TestConnectionCommand = new DelegateCommand(
            async () => await ExecuteTestAsync(), () => !IsBusy);

        TestTargetConnectionCommand = new DelegateCommand(
            async () => await ExecuteTestTargetAsync(), () => !IsBusy);

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

        if (_appConfig.IsConfigured
         && !string.IsNullOrWhiteSpace(_appConfig.ConnectionString))
        {
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

        // ── Load Target DB nếu đã cấu hình ──────────────────
        if (_appConfig.IsTargetConfigured
         && !string.IsNullOrWhiteSpace(_appConfig.TargetConnectionString))
        {
            try
            {
                var tb = new SqlConnectionStringBuilder(_appConfig.TargetConnectionString);
                TargetServer                 = tb.DataSource;
                TargetDatabase               = tb.InitialCatalog;
                TargetUserId                 = tb.UserID;
                TargetPassword               = tb.Password;
                TargetTrustServerCertificate = tb.TrustServerCertificate;
            }
            catch
            {
                // NOTE: Target connection string format lạ → giữ mặc định
            }
        }
    }

    /// <summary>Build connection string Config DB từ các trường riêng lẻ.</summary>
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

    /// <summary>Build connection string Target DB từ các trường riêng lẻ.</summary>
    private string BuildTargetConnectionString()
    {
        var builder = new SqlConnectionStringBuilder
        {
            DataSource             = TargetServer,
            InitialCatalog         = TargetDatabase,
            UserID                 = TargetUserId,
            Password               = TargetPassword,
            TrustServerCertificate = TargetTrustServerCertificate,
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
                SetStatus(success: true, "✓  Kết nối Config DB thành công!", visible: true, isError: false);
            else
                SetStatus(success: false, error, visible: true, isError: true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ExecuteTestTargetAsync()
    {
        IsBusy = true;
        SetTargetStatus(success: false, "Đang kiểm tra kết nối Target DB...", visible: true, isError: false);

        try
        {
            var error = await _appConfig.TestConnectionAsync(BuildTargetConnectionString());
            if (error is null)
                SetTargetStatus(success: true, "✓  Kết nối Target DB thành công!", visible: true, isError: false);
            else
                SetTargetStatus(success: false, error, visible: true, isError: true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ExecuteSaveAsync()
    {
        // ── 1. Test Config DB trước khi lưu ─────────────────
        IsBusy = true;
        SetStatus(success: false, "Đang kiểm tra kết nối Config DB...", visible: true, isError: false);

        try
        {
            var error = await _appConfig.TestConnectionAsync(BuildConnectionString());
            if (error is not null)
            {
                SetStatus(success: false, $"Lỗi kết nối Config DB — chưa lưu:\n{error}", visible: true, isError: true);
                return;
            }

            // ── 2. Ghi vào file (kèm Target DB nếu đã nhập) ─
            SetStatus(success: false, "Đang lưu...", visible: true, isError: false);

            // Chỉ lưu Target CS khi user đã nhập Database (tránh lưu CS rỗng)
            string? targetCs = string.IsNullOrWhiteSpace(TargetDatabase)
                ? null
                : BuildTargetConnectionString();

            await _appConfig.SaveAsync(BuildConnectionString(), TenantId, targetCs);
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

    private void SetTargetStatus(bool success, string message, bool visible, bool isError)
    {
        TargetStatusMessage    = message;
        IsTargetStatusSuccess  = success;
        IsTargetStatusError    = isError;
        IsTargetStatusVisible  = visible;
    }

    private void ClearStatus()       => SetStatus(false, "", false, false);
    private void ClearTargetStatus() => SetTargetStatus(false, "", false, false);
}
