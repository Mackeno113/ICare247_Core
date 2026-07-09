// File    : DocTemplateEditorViewModel.cs
// Module  : DocTemplate
// Layer   : Presentation
// Purpose : VM màn soạn fragment — nạp biến từ proc + chọn bộ mẫu/mảnh + nạp/lưu fragment vào Config DB.
//           Thao tác RichEdit (chèn field/hướng giấy/bytes) do code-behind View đảm nhiệm.
// Spec    : docs/spec/28_DOC_TEMPLATE_SPEC.md §5.4, §8.

using System.Collections.ObjectModel;
using ConfigStudio.WPF.UI.Core.Data;
using ConfigStudio.WPF.UI.Core.Interfaces;
using ConfigStudio.WPF.UI.Core.ViewModels;
using Prism.Commands;

namespace ConfigStudio.WPF.UI.Modules.DocTemplate.ViewModels;

/// <summary>Một đích lưu fragment: master của bộ mẫu, hoặc 1 mảnh detail.</summary>
public sealed record FragmentTarget(bool IsMaster, long Id, string Label)
{
    public override string ToString() => Label;
}

/// <summary>VM soạn fragment: nạp biến (cột proc) + quản lý bộ mẫu/mảnh + nạp/lưu bytes .docx.</summary>
public sealed class DocTemplateEditorViewModel : ViewModelBase
{
    private readonly ISchemaInspectorService _schema;
    private readonly IAppConfigService _config;
    private readonly IDocTemplateDataService _svc;

    public DocTemplateEditorViewModel(
        ISchemaInspectorService schema, IAppConfigService config, IDocTemplateDataService svc)
    {
        _schema = schema;
        _config = config;
        _svc = svc;
        LoadVariablesCommand  = new DelegateCommand(async () => await LoadVariablesAsync());
        LoadTemplatesCommand  = new DelegateCommand(async () => await LoadTemplatesAsync());
        CreateTemplateCommand = new DelegateCommand(async () => await CreateTemplateAsync());
        CreateDetailCommand   = new DelegateCommand(async () => await CreateDetailAsync());
    }

    // ── Biến (cột proc) ─────────────────────────────────────────────────────
    private string _procName = "";
    public string ProcName { get => _procName; set => SetProperty(ref _procName, value); }

    public ObservableCollection<ColumnSchemaDto> Variables { get; } = [];

    private ColumnSchemaDto? _selectedVariable;
    public ColumnSchemaDto? SelectedVariable { get => _selectedVariable; set => SetProperty(ref _selectedVariable, value); }

    public DelegateCommand LoadVariablesCommand { get; }

    // ── Bộ mẫu + mảnh ───────────────────────────────────────────────────────
    public ObservableCollection<DocTemplateListItem> Templates { get; } = [];

    private DocTemplateListItem? _selectedTemplate;
    public DocTemplateListItem? SelectedTemplate
    {
        get => _selectedTemplate;
        set { if (SetProperty(ref _selectedTemplate, value)) _ = LoadFragmentTargetsAsync(); }
    }

    /// <summary>Đích lưu: [Master] + các mảnh detail của bộ mẫu đang chọn.</summary>
    public ObservableCollection<FragmentTarget> FragmentTargets { get; } = [];

    private FragmentTarget? _selectedFragment;
    public FragmentTarget? SelectedFragment { get => _selectedFragment; set => SetProperty(ref _selectedFragment, value); }

    public DelegateCommand LoadTemplatesCommand { get; }

    // ── Tạo bộ mẫu / mảnh ───────────────────────────────────────────────────
    private string _newMa = "";
    public string NewMa { get => _newMa; set => SetProperty(ref _newMa, value); }

    private string _newTen = "";
    public string NewTen { get => _newTen; set => SetProperty(ref _newTen, value); }

    private string _newDetailProc = "";
    public string NewDetailProc { get => _newDetailProc; set => SetProperty(ref _newDetailProc, value); }

    public DelegateCommand CreateTemplateCommand { get; }
    public DelegateCommand CreateDetailCommand { get; }

    // ── Trạng thái ──────────────────────────────────────────────────────────
    private string _status = "";
    public string Status { get => _status; set => SetProperty(ref _status, value); }

    private bool _isBusy;
    public bool IsBusy { get => _isBusy; set => SetProperty(ref _isBusy, value); }

    /// <summary>Nạp cột kết quả proc (Target DB) làm danh sách biến.</summary>
    private async Task LoadVariablesAsync()
    {
        if (!_config.IsTargetConfigured) { Status = "Chưa cấu hình Target DB (Data DB) trong Cài đặt."; return; }
        if (string.IsNullOrWhiteSpace(ProcName)) { Status = "Nhập tên stored proc trước."; return; }
        try
        {
            IsBusy = true; Status = "Đang nạp biến…";
            var cols = await _schema.GetProcedureColumnsAsync(_config.TargetConnectionString!, "dbo", ProcName.Trim());
            Variables.Clear();
            foreach (var c in cols) Variables.Add(c);
            Status = $"Đã nạp {Variables.Count} biến từ '{ProcName.Trim()}'.";
        }
        catch (Exception ex) { Status = "Lỗi nạp biến: " + ex.Message; }
        finally { IsBusy = false; }
    }

    /// <summary>Nạp danh sách bộ mẫu (Config DB).</summary>
    private async Task LoadTemplatesAsync()
    {
        if (!_config.IsConfigured) { Status = "Chưa cấu hình Config DB."; return; }
        try
        {
            var list = await _svc.GetTemplatesAsync();
            Templates.Clear();
            foreach (var t in list) Templates.Add(t);
            Status = $"Có {Templates.Count} bộ mẫu.";
        }
        catch (Exception ex) { Status = "Lỗi nạp bộ mẫu: " + ex.Message; }
    }

    /// <summary>Dựng đích lưu (Master + các mảnh) cho bộ mẫu đang chọn.</summary>
    private async Task LoadFragmentTargetsAsync()
    {
        FragmentTargets.Clear();
        SelectedFragment = null;
        if (SelectedTemplate is null) return;
        FragmentTargets.Add(new FragmentTarget(true, SelectedTemplate.Id, "Master (A4 dọc)"));
        try
        {
            var details = await _svc.GetDetailsAsync(SelectedTemplate.Id);
            foreach (var d in details)
                FragmentTargets.Add(new FragmentTarget(false, d.Id, $"Detail: {d.Ma} — {d.Ten}"));
        }
        catch (Exception ex) { Status = "Lỗi nạp mảnh: " + ex.Message; }
        SelectedFragment = FragmentTargets.FirstOrDefault();
    }

    /// <summary>Tạo bộ mẫu mới (Mã/Tên + Master_Proc = ô ProcName).</summary>
    private async Task CreateTemplateAsync()
    {
        if (string.IsNullOrWhiteSpace(NewMa) || string.IsNullOrWhiteSpace(NewTen))
        { Status = "Nhập Mã + Tên bộ mẫu."; return; }
        if (string.IsNullOrWhiteSpace(ProcName))
        { Status = "Nhập stored proc master ở ô 'Stored proc'."; return; }
        try
        {
            var id = await _svc.CreateTemplateAsync(NewMa.Trim(), NewTen.Trim(), ProcName.Trim());
            await LoadTemplatesAsync();
            SelectedTemplate = Templates.FirstOrDefault(t => t.Id == id);
            Status = $"Đã tạo bộ mẫu '{NewMa.Trim()}'.";
            NewMa = ""; NewTen = "";
        }
        catch (Exception ex) { Status = "Lỗi tạo bộ mẫu: " + ex.Message; }
    }

    /// <summary>Thêm mảnh detail vào bộ mẫu đang chọn (Mã/Tên = ô mới, Detail_Proc = ô riêng).</summary>
    private async Task CreateDetailAsync()
    {
        if (SelectedTemplate is null) { Status = "Chọn bộ mẫu trước."; return; }
        if (string.IsNullOrWhiteSpace(NewMa) || string.IsNullOrWhiteSpace(NewTen) || string.IsNullOrWhiteSpace(NewDetailProc))
        { Status = "Nhập Mã + Tên + proc cho mảnh detail."; return; }
        try
        {
            var thuTu = FragmentTargets.Count;   // sau các mảnh hiện có
            await _svc.CreateDetailAsync(SelectedTemplate.Id, NewMa.Trim(), NewTen.Trim(), NewDetailProc.Trim(), thuTu);
            await LoadFragmentTargetsAsync();
            Status = $"Đã thêm mảnh '{NewMa.Trim()}'.";
            NewMa = ""; NewTen = ""; NewDetailProc = "";
        }
        catch (Exception ex) { Status = "Lỗi thêm mảnh: " + ex.Message; }
    }

    /// <summary>Nạp bytes fragment đang chọn (code-behind gọi để đổ vào RichEdit).</summary>
    public async Task<byte[]?> LoadCurrentFragmentAsync()
    {
        if (SelectedFragment is null) { Status = "Chọn đích (Master/mảnh) trước."; return null; }
        try
        {
            var bytes = SelectedFragment.IsMaster
                ? await _svc.GetMasterDocxAsync(SelectedFragment.Id)
                : await _svc.GetDetailDocxAsync(SelectedFragment.Id);
            Status = bytes is { Length: > 0 } ? "Đã nạp fragment từ DB." : "Fragment trống — soạn mới.";
            return bytes;
        }
        catch (Exception ex) { Status = "Lỗi nạp fragment: " + ex.Message; return null; }
    }

    /// <summary>Lưu bytes fragment đang soạn vào DB (code-behind cung cấp bytes từ RichEdit).</summary>
    public async Task<bool> SaveCurrentFragmentAsync(byte[] docx)
    {
        if (SelectedFragment is null) { Status = "Chọn đích (Master/mảnh) trước."; return false; }
        try
        {
            if (SelectedFragment.IsMaster) await _svc.SaveMasterDocxAsync(SelectedFragment.Id, docx);
            else await _svc.SaveDetailDocxAsync(SelectedFragment.Id, docx);
            Status = "Đã lưu fragment vào DB.";
            return true;
        }
        catch (Exception ex) { Status = "Lỗi lưu fragment: " + ex.Message; return false; }
    }
}
