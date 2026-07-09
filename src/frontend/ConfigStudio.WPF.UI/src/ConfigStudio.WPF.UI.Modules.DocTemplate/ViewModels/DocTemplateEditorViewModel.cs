// File    : DocTemplateEditorViewModel.cs
// Module  : DocTemplate
// Layer   : Presentation
// Purpose : VM màn soạn fragment — nạp danh sách biến (cột) từ stored proc trên Target DB để kéo/chèn.
//           Thao tác RichEdit (chèn field/đổi hướng giấy/nạp-lưu) do code-behind View đảm nhiệm.
// Spec    : docs/spec/28_DOC_TEMPLATE_SPEC.md §5.4, §8.

using System.Collections.ObjectModel;
using ConfigStudio.WPF.UI.Core.Data;
using ConfigStudio.WPF.UI.Core.Interfaces;
using ConfigStudio.WPF.UI.Core.ViewModels;
using Prism.Commands;

namespace ConfigStudio.WPF.UI.Modules.DocTemplate.ViewModels;

/// <summary>VM soạn 1 fragment template: nhập tên proc → nạp biến (cột kết quả) để chèn vào tài liệu.</summary>
public sealed class DocTemplateEditorViewModel : ViewModelBase
{
    private readonly ISchemaInspectorService _schema;
    private readonly IAppConfigService _config;

    public DocTemplateEditorViewModel(ISchemaInspectorService schema, IAppConfigService config)
    {
        _schema = schema;
        _config = config;
        LoadVariablesCommand = new DelegateCommand(async () => await LoadVariablesAsync());
    }

    private string _procName = "";
    /// <summary>Tên stored proc (Target DB) để lấy danh sách biến.</summary>
    public string ProcName
    {
        get => _procName;
        set => SetProperty(ref _procName, value);
    }

    /// <summary>Danh sách biến = cột kết quả proc.</summary>
    public ObservableCollection<ColumnSchemaDto> Variables { get; } = [];

    private ColumnSchemaDto? _selectedVariable;
    /// <summary>Biến đang chọn — code-behind chèn MERGEFIELD theo cột này.</summary>
    public ColumnSchemaDto? SelectedVariable
    {
        get => _selectedVariable;
        set => SetProperty(ref _selectedVariable, value);
    }

    private string _status = "";
    /// <summary>Thông báo trạng thái (lỗi/nạp xong) hiển thị ở thanh dưới.</summary>
    public string Status
    {
        get => _status;
        set => SetProperty(ref _status, value);
    }

    private bool _isBusy;
    public bool IsBusy
    {
        get => _isBusy;
        set => SetProperty(ref _isBusy, value);
    }

    /// <summary>Nạp biến từ proc.</summary>
    public DelegateCommand LoadVariablesCommand { get; }

    /// <summary>
    /// Gọi <c>sp_describe/dm_exec_describe</c> (qua SchemaInspector) trên Target DB → cột kết quả proc.
    /// Sự kiện theo sau: đổ vào <see cref="Variables"/>; lỗi → hiện ở <see cref="Status"/>.
    /// </summary>
    private async Task LoadVariablesAsync()
    {
        if (!_config.IsTargetConfigured)
        {
            Status = "Chưa cấu hình Target DB (Cài đặt → chuỗi kết nối Data DB).";
            return;
        }
        if (string.IsNullOrWhiteSpace(ProcName))
        {
            Status = "Nhập tên stored proc trước.";
            return;
        }

        try
        {
            IsBusy = true;
            Status = "Đang nạp biến…";
            var cols = await _schema.GetProcedureColumnsAsync(
                _config.TargetConnectionString!, "dbo", ProcName.Trim());
            Variables.Clear();
            foreach (var c in cols) Variables.Add(c);
            Status = $"Đã nạp {Variables.Count} biến từ '{ProcName.Trim()}'.";
        }
        catch (Exception ex)
        {
            Status = "Lỗi nạp biến: " + ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }
}
