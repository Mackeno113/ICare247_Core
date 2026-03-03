// File    : AstNodeViewModel.cs
// Module  : Grammar
// Layer   : Presentation
// Purpose : ViewModel bọc AstNode cho TreeView binding — hỗ trợ select, expand, hiển thị icon.

using System.Collections.ObjectModel;
using Prism.Mvvm;

namespace ConfigStudio.WPF.UI.Modules.Grammar.Models;

/// <summary>
/// ViewModel bọc <see cref="AstNode"/> để bind vào TreeView.
/// Cung cấp <see cref="DisplayText"/>, <see cref="IconKind"/>, và <see cref="Children"/> cho HierarchicalDataTemplate.
/// </summary>
public sealed class AstNodeViewModel : BindableBase
{
    public AstNode Node { get; set; } = null!;
    public ObservableCollection<AstNodeViewModel> Children { get; set; } = [];

    /// <summary>
    /// Node cha — dùng để navigate tree khi insert/delete node.
    /// </summary>
    public AstNodeViewModel? Parent { get; set; }

    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    private bool _isExpanded = true;
    public bool IsExpanded
    {
        get => _isExpanded;
        set => SetProperty(ref _isExpanded, value);
    }

    /// <summary>
    /// Độ sâu node trong tree (root = 0). Dùng cho validation max depth.
    /// </summary>
    public int Depth { get; set; }

    /// <summary>
    /// Text hiển thị trên TreeView — computed từ <see cref="AstNode.Type"/> và dữ liệu node.
    /// </summary>
    public string DisplayText => Node.Type switch
    {
        AstNodeType.Literal => $"Literal: {Node.Value ?? "null"} ({Node.NetType})",
        AstNodeType.Identifier => $"Identifier: {Node.Name}",
        AstNodeType.Binary => $"Binary: {Node.Operator}",
        AstNodeType.Unary => $"Unary: {Node.Operator}",
        AstNodeType.Function => $"Function: {Node.FunctionName}({Node.Arguments.Count} args)",
        AstNodeType.CustomHandler => $"CustomHandler: {Node.HandlerCode}",
        _ => "Unknown"
    };

    /// <summary>
    /// MaterialDesign PackIcon Kind cho từng loại node.
    /// </summary>
    public string IconKind => Node.Type switch
    {
        AstNodeType.Literal => "FileDocument",
        AstNodeType.Identifier => "MapMarker",
        AstNodeType.Binary => "Cog",
        AstNodeType.Unary => "ExclamationThick",
        AstNodeType.Function => "Function",
        AstNodeType.CustomHandler => "CodeBraces",
        _ => "Help"
    };

    /// <summary>
    /// Refresh lại <see cref="DisplayText"/> khi node data thay đổi.
    /// </summary>
    public void RefreshDisplay()
    {
        RaisePropertyChanged(nameof(DisplayText));
        RaisePropertyChanged(nameof(IconKind));
    }
}
