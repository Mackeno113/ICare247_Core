// File    : DocTemplateEditorView.xaml.cs
// Module  : DocTemplate
// Layer   : Presentation
// Purpose : Code-behind thao tác RichEditControl (mở/lưu .docx, chèn MERGEFIELD, đổi hướng giấy).
//           Document control khó MVVM thuần → thao tác tài liệu để ở code-behind (chuẩn DevExpress).
// Spec    : docs/spec/28_DOC_TEMPLATE_SPEC.md §8.2.

using System.IO;
using System.Windows;
using ConfigStudio.WPF.UI.Modules.DocTemplate.ViewModels;
using DevExpress.XtraRichEdit;
// WinForms bật (RichEdit interop) → alias rõ WPF, tránh xung đột UserControl/Dialog với System.Windows.Forms.
using UserControl = System.Windows.Controls.UserControl;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;

namespace ConfigStudio.WPF.UI.Modules.DocTemplate.Views;

/// <summary>Màn soạn 1 fragment template: RichEdit + panel biến. Chèn field/đổi hướng giấy tại code-behind.</summary>
public partial class DocTemplateEditorView : UserControl
{
    public DocTemplateEditorView() => InitializeComponent();

    private DocTemplateEditorViewModel? Vm => DataContext as DocTemplateEditorViewModel;

    /// <summary>Mở 1 file .docx vào editor. Sự kiện theo sau: nạp document (OpenXml).</summary>
    private void Open_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog { Filter = "Word (*.docx)|*.docx" };
        if (dlg.ShowDialog() == true)
            RichEdit.LoadDocument(dlg.FileName, DocumentFormat.OpenXml);
    }

    /// <summary>Lưu document hiện tại ra .docx. Sự kiện theo sau: ghi file OpenXml.</summary>
    private void Save_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new SaveFileDialog { Filter = "Word (*.docx)|*.docx", FileName = "fragment.docx" };
        if (dlg.ShowDialog() == true)
            RichEdit.SaveDocument(dlg.FileName, DocumentFormat.OpenXml);
    }

    /// <summary>Chèn MERGEFIELD của biến đang chọn tại vị trí con trỏ. Sự kiện theo sau: cập nhật field.</summary>
    private void InsertField_Click(object sender, RoutedEventArgs e)
    {
        var v = Vm?.SelectedVariable;
        if (v is null) return;
        var doc = RichEdit.Document;
        doc.Fields.Create(doc.CaretPosition, $"MERGEFIELD {v.ColumnName}");
        doc.Fields.Update();
    }

    /// <summary>Đổi hướng giấy A4 dọc ↔ ngang cho section hiện tại. Sự kiện theo sau: cập nhật layout.</summary>
    private void ToggleOrientation_Click(object sender, RoutedEventArgs e)
    {
        var page = RichEdit.Document.Sections[0].Page;
        page.Landscape = !page.Landscape;
    }

    /// <summary>Nạp bytes fragment đích (Master/mảnh) từ DB vào editor. Sự kiện theo sau: LoadDocument.</summary>
    private async void LoadDb_Click(object sender, RoutedEventArgs e)
    {
        if (Vm is null) return;
        var bytes = await Vm.LoadCurrentFragmentAsync();
        if (bytes is { Length: > 0 })
            RichEdit.LoadDocument(bytes, DocumentFormat.OpenXml);
    }

    /// <summary>Lưu document hiện tại (bytes) vào fragment đích trong DB. Sự kiện theo sau: SaveCurrentFragmentAsync.</summary>
    private async void SaveDb_Click(object sender, RoutedEventArgs e)
    {
        if (Vm is null) return;
        using var ms = new MemoryStream();
        RichEdit.SaveDocument(ms, DocumentFormat.OpenXml);
        await Vm.SaveCurrentFragmentAsync(ms.ToArray());
    }
}
