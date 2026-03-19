// File    : PublishChecklistViewModel.cs
// Module  : Forms
// Layer   : Presentation
// Purpose : ViewModel cho Publish Checklist (Screen 11) — kiểm tra trước khi publish form.

using System.Collections.ObjectModel;
using ConfigStudio.WPF.UI.Core.Constants;
using ConfigStudio.WPF.UI.Core.ViewModels;
using ConfigStudio.WPF.UI.Modules.Forms.Models;
using Prism.Commands;
using Prism.Navigation.Regions;

namespace ConfigStudio.WPF.UI.Modules.Forms.ViewModels;

/// <summary>
/// ViewModel cho Publish Checklist (Screen 11).
/// Chạy danh sách checks trước khi publish form: label keys, expression, whitelist, depth, i18n...
/// </summary>
public sealed class PublishChecklistViewModel : ViewModelBase, INavigationAware
{
    private readonly IRegionManager _regionManager;

    // ── Form context ─────────────────────────────────────────
    private int _formId;

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

    public PublishChecklistViewModel(IRegionManager regionManager)
    {
        _regionManager = regionManager;

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
        FormCode = navigationContext.Parameters.GetValue<string>("formCode") ?? "PURCHASE_ORDER";

        InitChecklist();
    }

    public bool IsNavigationTarget(NavigationContext navigationContext) => false;
    public void OnNavigatedFrom(NavigationContext navigationContext) { }

    // ── Init checklist items ─────────────────────────────────

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
            Description = "Return type của calculate = compatible với target field"
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
        Summary = "Chưa chạy kiểm tra.";
    }

    // ── Run all checks (mock) ────────────────────────────────

    /// <summary>
    /// Chạy tất cả checks — mock kết quả cho demo.
    /// Sau này sẽ gọi API validate từng item.
    /// </summary>
    private async Task ExecuteRunAllChecksAsync()
    {
        IsRunning = true;

        try
        {
            // ── Set tất cả = Running ─────────────────────────────
            foreach (var item in ChecklistItems)
            {
                item.Status = CheckStatus.Running;
                item.Detail = null;
            }

            // ── Simulate check từng item (mock delay) ────────────
            foreach (var item in ChecklistItems)
            {
                await Task.Delay(200);
                RunMockCheck(item);
            }

            // ── Tổng kết ────────────────────────────────────────
            IssueCount = ChecklistItems.Count(i => i.Status is CheckStatus.Failed or CheckStatus.Warning);
            AllPassed = IssueCount == 0;
            Summary = AllPassed
                ? "Tất cả kiểm tra đạt. Sẵn sàng Publish."
                : $"{IssueCount} issue cần sửa trước khi Publish.";
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

    /// <summary>
    /// Mock check result — 1 item fail (thiếu bản dịch EN) để demo.
    /// </summary>
    private static void RunMockCheck(ChecklistItem item)
    {
        // NOTE: Mock — hầu hết pass, 1 item fail để demo
        if (item.Description.Contains("Error_Key"))
        {
            item.Status = CheckStatus.Failed;
            item.Detail = "Field SoLuong: Error_Key 'err.soluong.range' thiếu bản dịch EN";
        }
        else
        {
            item.Status = CheckStatus.Passed;
        }
    }

    // ── Command handlers ─────────────────────────────────────

    private void ExecutePublish()
    {
        // TODO(phase2): Gọi publish service → invalidate cache → navigate về FormManager
        var p = new NavigationParameters();
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
