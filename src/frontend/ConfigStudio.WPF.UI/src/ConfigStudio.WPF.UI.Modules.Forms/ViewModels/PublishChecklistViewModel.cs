// File    : PublishChecklistViewModel.cs
// Module  : Forms
// Layer   : Presentation
// Purpose : ViewModel cho Publish Checklist (Screen 11) — kiểm tra trước khi publish form.

using System.Collections.ObjectModel;
using ConfigStudio.WPF.UI.Core.Constants;
using ConfigStudio.WPF.UI.Core.Interfaces;
using ConfigStudio.WPF.UI.Core.ViewModels;
using ConfigStudio.WPF.UI.Modules.Forms.Models;
using Prism.Commands;
using Prism.Navigation.Regions;

namespace ConfigStudio.WPF.UI.Modules.Forms.ViewModels;

/// <summary>
/// ViewModel cho Publish Checklist (Screen 11).
/// Chạy 11 checks thật qua IPublishCheckService trước khi publish form.
/// </summary>
public sealed class PublishChecklistViewModel : ViewModelBase, INavigationAware
{
    private readonly IRegionManager _regionManager;
    private readonly IPublishCheckService _checkService;

    // ── Form context ─────────────────────────────────────────
    private int _formId;
    private int _tenantId = 1;

    private string _formCode = "";
    public string FormCode { get => _formCode; set => SetProperty(ref _formCode, value); }

    // ── Checklist ────────────────────────────────────────────
    public ObservableCollection<ChecklistItem> ChecklistItems { get; } = [];

    private bool _allPassed;
    public bool AllPassed
    {
        get => _allPassed;
        set => SetProperty(ref _allPassed, value);
    }

    private bool _isRunning;
    public bool IsRunning
    {
        get => _isRunning;
        set => SetProperty(ref _isRunning, value);
    }

    private int _issueCount;
    public int IssueCount
    {
        get => _issueCount;
        set => SetProperty(ref _issueCount, value);
    }

    private string _summary = "";
    public string Summary
    {
        get => _summary;
        set => SetProperty(ref _summary, value);
    }

    // ── Commands ─────────────────────────────────────────────
    public DelegateCommand RunAllChecksCommand { get; }
    public DelegateCommand PublishCommand { get; }
    public DelegateCommand<ChecklistItem> JumpToCommand { get; }
    public DelegateCommand BackCommand { get; }

    public PublishChecklistViewModel(
        IRegionManager regionManager,
        IPublishCheckService checkService)
    {
        _regionManager = regionManager;
        _checkService  = checkService;

        RunAllChecksCommand = new DelegateCommand(async () => await ExecuteRunAllChecksAsync(), () => !IsRunning)
            .ObservesProperty(() => IsRunning);
        PublishCommand = new DelegateCommand(ExecutePublish, () => AllPassed)
            .ObservesProperty(() => AllPassed);
        JumpToCommand = new DelegateCommand<ChecklistItem>(ExecuteJumpTo);
        BackCommand = new DelegateCommand(ExecuteBack);
    }

    // ── INavigationAware ─────────────────────────────────────

    public void OnNavigatedTo(NavigationContext navigationContext)
    {
        _formId = navigationContext.Parameters.GetValue<int>("formId");
        FormCode = navigationContext.Parameters.GetValue<string>("formCode") ?? "";

        // NOTE: TenantId có thể được pass qua nav params — mặc định 1
        var tenantId = navigationContext.Parameters.GetValue<int>("tenantId");
        if (tenantId > 0) _tenantId = tenantId;

        InitChecklist();
    }

    public bool IsNavigationTarget(NavigationContext navigationContext) => false;
    public void OnNavigatedFrom(NavigationContext navigationContext) { }

    // ── Init checklist items ─────────────────────────────────

    /// <summary>
    /// Khởi tạo 11 checklist items với trạng thái ban đầu.
    /// </summary>
    private void InitChecklist()
    {
        ChecklistItems.Clear();

        ChecklistItems.Add(new ChecklistItem
        {
            Description = "Tất cả field có Label_Key hợp lệ",
            JumpToView = ViewNames.FormEditor
        });
        ChecklistItems.Add(new ChecklistItem
        {
            Description = "Tất cả Expression_Json parse thành công"
        });
        ChecklistItems.Add(new ChecklistItem
        {
            Description = "Tất cả function trong expression có trong Gram_Function whitelist",
            JumpToView = ViewNames.GrammarLibrary
        });
        ChecklistItems.Add(new ChecklistItem
        {
            Description = "Tất cả operator trong expression có trong Gram_Operator whitelist",
            JumpToView = ViewNames.GrammarLibrary
        });
        ChecklistItems.Add(new ChecklistItem
        {
            Description = "Return type của rule expression = Boolean"
        });
        ChecklistItems.Add(new ChecklistItem
        {
            Description = "Return type của calculate compatible với target field"
        });
        ChecklistItems.Add(new ChecklistItem
        {
            Description = "Không có circular dependency",
            JumpToView = ViewNames.DependencyViewer
        });
        ChecklistItems.Add(new ChecklistItem
        {
            Description = "Tất cả AST depth ≤ 20"
        });
        ChecklistItems.Add(new ChecklistItem
        {
            Description = "Tất cả Error_Key có bản dịch đầy đủ các ngôn ngữ",
            JumpToView = ViewNames.I18nManager
        });
        ChecklistItems.Add(new ChecklistItem
        {
            Description = "Tất cả CallAPI URL có format hợp lệ"
        });
        ChecklistItems.Add(new ChecklistItem
        {
            Description = "Sys_Dependency đầy đủ cho cross-field references",
            JumpToView = ViewNames.DependencyViewer
        });

        AllPassed = false;
        IssueCount = 0;
        Summary = "Nhấn 'Chạy kiểm tra' để bắt đầu.";
    }

    // ── Run all checks ───────────────────────────────────────

    /// <summary>
    /// Gọi từng check thật qua IPublishCheckService, cập nhật status từng item.
    /// </summary>
    private async Task ExecuteRunAllChecksAsync()
    {
        IsRunning = true;
        AllPassed = false;

        try
        {
            // ── Reset tất cả về Running ──────────────────────
            foreach (var item in ChecklistItems)
            {
                item.Status = CheckStatus.Running;
                item.Detail = null;
            }

            // ── Thứ tự check tương ứng với InitChecklist() ──
            Func<int, int, CancellationToken, Task<Core.Interfaces.CheckResult>>[] checks =
            [
                _checkService.CheckLabelKeysAsync,
                _checkService.CheckExpressionsParseAsync,
                _checkService.CheckFunctionWhitelistAsync,
                _checkService.CheckOperatorWhitelistAsync,
                _checkService.CheckRuleReturnTypeAsync,
                _checkService.CheckCalculateReturnTypeAsync,
                _checkService.CheckCircularDependencyAsync,
                _checkService.CheckAstDepthAsync,
                _checkService.CheckI18nCompletenessAsync,
                _checkService.CheckCallApiUrlsAsync,
                _checkService.CheckDependencyGraphAsync
            ];

            for (int i = 0; i < checks.Length && i < ChecklistItems.Count; i++)
            {
                var item = ChecklistItems[i];
                try
                {
                    var result = await checks[i](_formId, _tenantId, default);
                    ApplyResult(item, result);
                }
                catch (Exception ex)
                {
                    item.Status = CheckStatus.Failed;
                    item.Detail = $"Lỗi: {ex.Message}";
                }
            }

            // ── Tổng kết ────────────────────────────────────
            IssueCount = ChecklistItems.Count(i => i.Status == CheckStatus.Failed);
            var warningCount = ChecklistItems.Count(i => i.Status == CheckStatus.Warning);
            AllPassed = IssueCount == 0;

            Summary = AllPassed
                ? warningCount > 0
                    ? $"Không có lỗi. {warningCount} cảnh báo — có thể Publish."
                    : "Tất cả kiểm tra đạt. Sẵn sàng Publish."
                : $"{IssueCount} lỗi cần sửa trước khi Publish.{(warningCount > 0 ? $" ({warningCount} cảnh báo)" : "")}";
        }
        catch (Exception ex)
        {
            Summary = $"Lỗi khi chạy kiểm tra: {ex.Message}";
            AllPassed = false;
        }
        finally
        {
            IsRunning = false;
        }
    }

    /// <summary>Map CheckResult sang ChecklistItem.Status.</summary>
    private static void ApplyResult(ChecklistItem item, Core.Interfaces.CheckResult result)
    {
        if (result.Passed && !result.IsWarning)
        {
            item.Status = CheckStatus.Passed;
            item.Detail = null;
        }
        else if (result.IsWarning)
        {
            item.Status = CheckStatus.Warning;
            item.Detail = result.Detail;
        }
        else
        {
            item.Status = CheckStatus.Failed;
            item.Detail = result.Detail;
        }
    }

    // ── Command handlers ─────────────────────────────────────

    private void ExecutePublish()
    {
        // NOTE: Publish = xác nhận form sẵn sàng → navigate về FormManager
        // Nếu sau này có trạng thái Published riêng → gọi service ở đây
        var p = new NavigationParameters { { "formId", _formId } };
        _regionManager.RequestNavigate(RegionNames.Content, ViewNames.FormManager, p);
    }

    private void ExecuteJumpTo(ChecklistItem? item)
    {
        if (item?.JumpToView is null) return;

        var p = new NavigationParameters { { "formId", _formId } };
        if (item.JumpToParams is not null)
        {
            foreach (var kvp in item.JumpToParams)
                p.Add(kvp.Key, kvp.Value);
        }

        _regionManager.RequestNavigate(RegionNames.Content, item.JumpToView, p);
    }

    private void ExecuteBack()
    {
        var p = new NavigationParameters { { "formId", _formId } };
        _regionManager.RequestNavigate(RegionNames.Content, ViewNames.FormEditor, p);
    }
}
