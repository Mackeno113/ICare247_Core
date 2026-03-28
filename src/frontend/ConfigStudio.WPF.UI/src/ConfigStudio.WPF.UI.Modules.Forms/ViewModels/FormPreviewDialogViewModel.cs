// File    : FormPreviewDialogViewModel.cs
// Module  : Forms
// Layer   : Presentation
// Purpose : ViewModel cho FormPreviewDialog — load section/field metadata, render form preview.
//           Nhận FormId + FormName qua DialogParameters từ FormManagerViewModel.

using System.Collections.ObjectModel;
using ConfigStudio.WPF.UI.Core.Interfaces;
using ConfigStudio.WPF.UI.Core.ViewModels;
using ConfigStudio.WPF.UI.Modules.Forms.Models;
using Prism.Commands;
using Prism.Dialogs;

namespace ConfigStudio.WPF.UI.Modules.Forms.ViewModels;

/// <summary>
/// Dialog xem trước form: hiển thị sections + fields theo EditorType và label.
/// Chế độ read-only — không có tương tác data.
/// </summary>
public sealed class FormPreviewDialogViewModel : ViewModelBase, IDialogAware
{
    private readonly IFormDetailDataService? _detailService;
    private readonly IAppConfigService? _appConfig;

    // ── IDialogAware ─────────────────────────────────────────
    public DialogCloseListener RequestClose { get; set; }
    public string Title => $"Preview — {FormName}";

    // ── Header ───────────────────────────────────────────────
    private string _formName = "";
    public string FormName
    {
        get => _formName;
        private set { if (SetProperty(ref _formName, value)) RaisePropertyChanged(nameof(Title)); }
    }

    private string _formCode = "";
    public string FormCode { get => _formCode; private set => SetProperty(ref _formCode, value); }

    private int _formId;
    public int FormId { get => _formId; private set => SetProperty(ref _formId, value); }

    // ── Loading state ────────────────────────────────────────
    private bool _isLoading;
    public bool IsLoading { get => _isLoading; private set => SetProperty(ref _isLoading, value); }

    private string _loadError = "";
    public string LoadError { get => _loadError; private set => SetProperty(ref _loadError, value); }
    public bool HasLoadError => !string.IsNullOrEmpty(_loadError);

    // ── Data ─────────────────────────────────────────────────
    public ObservableCollection<SectionPreviewModel> Sections { get; } = [];

    /// <summary>Tổng số fields trong tất cả sections.</summary>
    public int TotalFieldCount => Sections.Sum(s => s.FieldCount);

    // ── Commands ─────────────────────────────────────────────
    public DelegateCommand CloseCommand { get; }

    public FormPreviewDialogViewModel(
        IFormDetailDataService? detailService = null,
        IAppConfigService? appConfig = null)
    {
        _detailService = detailService;
        _appConfig     = appConfig;

        CloseCommand = new DelegateCommand(
            () => RequestClose.Invoke(new DialogResult(ButtonResult.Cancel)));
    }

    // ── IDialogAware ─────────────────────────────────────────

    public bool CanCloseDialog() => !IsLoading;
    public void OnDialogClosed() { }

    public async void OnDialogOpened(IDialogParameters parameters)
    {
        FormId   = parameters.GetValue<int>("formId");
        FormName = parameters.GetValue<string>("formName") ?? "";
        FormCode = parameters.GetValue<string>("formCode") ?? "";

        await LoadPreviewAsync();
    }

    // ── Load data ────────────────────────────────────────────

    private async Task LoadPreviewAsync()
    {
        if (_detailService is null || _appConfig is not { IsConfigured: true })
        {
            LoadError = "Chưa cấu hình kết nối DB.";
            RaisePropertyChanged(nameof(HasLoadError));
            return;
        }

        IsLoading = true;
        LoadError = "";
        Sections.Clear();

        try
        {
            var ct = new CancellationTokenSource(TimeSpan.FromSeconds(20)).Token;
            var tenantId = _appConfig.TenantId;

            // Load song song sections + fields
            var sectionsTask = _detailService.GetSectionsByFormAsync(FormId, tenantId, ct);
            var fieldsTask   = _detailService.GetFieldsByFormAsync(FormId, tenantId, ct);

            await Task.WhenAll(sectionsTask, fieldsTask);

            var sections = await sectionsTask;
            var fields   = await fieldsTask;

            // Group fields by SectionCode
            var fieldsBySectionCode = fields
                .GroupBy(f => f.SectionCode ?? "", StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.OrderBy(f => f.OrderNo).ToList(),
                    StringComparer.OrdinalIgnoreCase);

            // Build SectionPreviewModel (theo thứ tự OrderNo)
            foreach (var s in sections.OrderBy(s => s.OrderNo))
            {
                var title = string.IsNullOrWhiteSpace(s.TitleKey) ? s.SectionCode : s.TitleKey;
                var model = new SectionPreviewModel
                {
                    SectionCode  = s.SectionCode,
                    DisplayTitle = title
                };

                if (fieldsBySectionCode.TryGetValue(s.SectionCode, out var sectionFields))
                {
                    foreach (var f in sectionFields)
                    {
                        model.Fields.Add(new FieldPreviewModel
                        {
                            FieldId    = f.FieldId,
                            LabelKey   = f.LabelKey,
                            ColumnCode = f.ColumnCode,
                            EditorType = f.EditorType,
                            IsReadOnly = f.IsReadOnly,
                            IsVisible  = f.IsVisible,
                            RuleCount  = f.RuleCount
                        });
                    }
                }

                Sections.Add(model);
            }

            // Thêm section "Chưa phân section" cho fields không có section
            if (fieldsBySectionCode.TryGetValue("", out var orphanFields) && orphanFields.Count > 0)
            {
                var orphan = new SectionPreviewModel
                {
                    SectionCode  = "__orphan__",
                    DisplayTitle = "Chưa phân section"
                };
                foreach (var f in orphanFields)
                    orphan.Fields.Add(new FieldPreviewModel
                    {
                        FieldId = f.FieldId, LabelKey = f.LabelKey,
                        ColumnCode = f.ColumnCode, EditorType = f.EditorType,
                        IsReadOnly = f.IsReadOnly, IsVisible = f.IsVisible,
                        RuleCount = f.RuleCount
                    });
                Sections.Add(orphan);
            }

            RaisePropertyChanged(nameof(TotalFieldCount));
        }
        catch (OperationCanceledException)
        {
            LoadError = "Tải dữ liệu quá thời gian — thử lại sau.";
            RaisePropertyChanged(nameof(HasLoadError));
        }
        catch (Exception ex)
        {
            LoadError = $"Lỗi tải preview: {ex.Message}";
            RaisePropertyChanged(nameof(HasLoadError));
        }
        finally
        {
            IsLoading = false;
        }
    }
}
