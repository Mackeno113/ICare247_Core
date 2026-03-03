// File    : DependencyEdge.cs
// Module  : Grammar
// Layer   : Presentation
// Purpose : Edge (cạnh) trong dependency graph — kết nối giữa 2 nodes.

using Prism.Mvvm;

namespace ConfigStudio.WPF.UI.Modules.Grammar.Models;

/// <summary>
/// Một edge trong dependency graph — kết nối Source → Target.
/// Hiển thị dưới dạng arrow line trên Canvas.
/// </summary>
public sealed class DependencyEdge : BindableBase
{
    public string SourceId { get; set; } = "";
    public string TargetId { get; set; } = "";

    /// <summary>
    /// Nhãn mô tả quan hệ — "validates", "triggers", "references", "calculates".
    /// </summary>
    public string Label { get; set; } = "";

    /// <summary>
    /// True nếu edge thuộc circular dependency → hiển thị màu đỏ.
    /// </summary>
    public bool IsCircular { get; set; }

    // ── Computed positions — tính từ node positions ──────────
    private double _x1;
    public double X1 { get => _x1; set => SetProperty(ref _x1, value); }

    private double _y1;
    public double Y1 { get => _y1; set => SetProperty(ref _y1, value); }

    private double _x2;
    public double X2 { get => _x2; set => SetProperty(ref _x2, value); }

    private double _y2;
    public double Y2 { get => _y2; set => SetProperty(ref _y2, value); }

    /// <summary>
    /// Màu edge: đỏ nếu circular, xám mặc định.
    /// </summary>
    public string EdgeColor => IsCircular ? "#EF5350" : "#78909C";
}
