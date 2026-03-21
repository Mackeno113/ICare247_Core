// File    : I18nManagerView.xaml.cs
// Module  : I18n
// Layer   : Presentation
// Purpose : Code-behind I18nManagerView — xử lý CellValueChanged để save inline edit vào DB.

using System.Windows.Controls;
using ConfigStudio.WPF.UI.Modules.I18n.Models;
using ConfigStudio.WPF.UI.Modules.I18n.ViewModels;
using DevExpress.Xpf.Grid;

namespace ConfigStudio.WPF.UI.Modules.I18n.Views;

public partial class I18nManagerView : UserControl
{
    public I18nManagerView()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Khi user commit giá trị trên cell vi-VN / en-US / ja-JP → gọi SaveCellCommand để save DB.
    /// </summary>
    private void OnCellValueChanged(object sender, CellValueChangedEventArgs e)
    {
        if (DataContext is not I18nManagerViewModel vm) return;
        if (e.Row is not I18nEntryDto entry) return;

        // Map FieldName → language code
        var lang = e.Column.FieldName switch
        {
            "ViVn" => "vi",
            "EnUs" => "en",
            "JaJp" => "ja",
            _      => null
        };

        if (lang is null) return;

        vm.SaveCellCommand.Execute(new CellSaveArgs(entry.ResourceKey, lang, e.Value?.ToString()));
    }
}
