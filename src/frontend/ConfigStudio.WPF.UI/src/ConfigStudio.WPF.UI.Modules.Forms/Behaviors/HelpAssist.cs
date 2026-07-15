// File    : HelpAssist.cs
// Module  : Forms / Behaviors
// Layer   : Presentation
// Purpose : Attached property gắn ToolTip hướng dẫn chi tiết (FieldHelpTopic) lên ô cấu hình.
//           XAML khai help:HelpAssist.Topic="key" hoặc help:HelpAssist.Prop="{Binding}".

using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Media;
using ConfigStudio.WPF.UI.Modules.Forms.Models;

namespace ConfigStudio.WPF.UI.Modules.Forms.Behaviors;

/// <summary>
/// Gắn ToolTip "hướng dẫn sử dụng" giàu nội dung lên bất kỳ FrameworkElement nào.
/// 2 cách dùng:
/// <c>help:HelpAssist.Topic="fk.valueField"</c> — tra <see cref="FieldHelpCatalog"/> theo key tĩnh;
/// <c>help:HelpAssist.Prop="{Binding}"</c> — cho dynamic props form, tra key "prop.{PropName}",
/// không có thì tự dựng topic từ Label/Description của definition.
/// Sự kiện theo sau: user trỏ chuột vào ô → WPF mở ToolTip đã dựng sẵn.
/// </summary>
public static class HelpAssist
{
    // ── Attached property: Topic (key tĩnh) ─────────────────────────

    public static readonly DependencyProperty TopicProperty =
        DependencyProperty.RegisterAttached(
            "Topic", typeof(string), typeof(HelpAssist),
            new PropertyMetadata(null, OnTopicChanged));

    public static string? GetTopic(DependencyObject obj) => (string?)obj.GetValue(TopicProperty);
    public static void SetTopic(DependencyObject obj, string? value) => obj.SetValue(TopicProperty, value);

    /// <summary>Key đổi → tra catalog và gắn ToolTip; key lạ thì bỏ qua (không ném lỗi).</summary>
    private static void OnTopicChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not FrameworkElement fe || e.NewValue is not string key) return;
        if (FieldHelpCatalog.TryGet(key, out var topic))
            Attach(fe, topic);
    }

    // ── Attached property: Prop (dynamic props form) ─────────────────

    public static readonly DependencyProperty PropProperty =
        DependencyProperty.RegisterAttached(
            "Prop", typeof(ControlPropValue), typeof(HelpAssist),
            new PropertyMetadata(null, OnPropChanged));

    public static ControlPropValue? GetProp(DependencyObject obj) => (ControlPropValue?)obj.GetValue(PropProperty);
    public static void SetProp(DependencyObject obj, ControlPropValue? value) => obj.SetValue(PropProperty, value);

    /// <summary>
    /// Binding dynamic prop đổi → ưu tiên topic soạn sẵn "prop.{PropName}";
    /// chưa có trong kho thì fallback dựng topic tối thiểu từ Label + Description.
    /// </summary>
    private static void OnPropChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not FrameworkElement fe || e.NewValue is not ControlPropValue prop) return;

        var def = prop.Definition;
        if (!FieldHelpCatalog.TryGet($"prop.{def.PropName}", out var topic))
        {
            var purpose = string.IsNullOrWhiteSpace(def.Description) ? def.PropName : def.Description!;
            topic = new FieldHelpTopic(
                Title:   $"{def.PropName} — {def.Label}",
                Purpose: purpose,
                HowTo:   def.AllowedValues is { Count: > 0 }
                             ? [$"Giá trị cho phép: {string.Join(" · ", def.AllowedValues)}"]
                             : []);
        }
        Attach(fe, topic);
    }

    // ── Dựng ToolTip ─────────────────────────────────────────────────

    /// <summary>
    /// Gắn ToolTip đã style theo DesignTokens (ADR-031) + kéo dài thời gian hiển thị
    /// để user kịp đọc hết hướng dẫn. Đặt Cursor=Help làm dấu hiệu "ô này có hướng dẫn".
    /// </summary>
    private static void Attach(FrameworkElement fe, FieldHelpTopic topic)
    {
        var tip = new ToolTip
        {
            Content         = BuildContent(topic),
            MaxWidth        = 460,
            Padding         = new Thickness(14, 12, 14, 12),
            HasDropShadow   = true,
            BorderThickness = new Thickness(1),
            Placement       = PlacementMode.Bottom,
        };
        tip.SetResourceReference(Control.BackgroundProperty,  "AppSurfaceBrush");
        tip.SetResourceReference(Control.BorderBrushProperty, "AppBorderStrongBrush");
        tip.SetResourceReference(Control.ForegroundProperty,  "AppTextBrush");

        fe.ToolTip = tip;
        ToolTipService.SetInitialShowDelay(fe, 400);
        ToolTipService.SetShowDuration(fe, 120_000);
        ToolTipService.SetBetweenShowDelay(fe, 200);
        if (fe.Cursor is null) fe.Cursor = System.Windows.Input.Cursors.Help;
    }

    /// <summary>Dựng cây visual nội dung tooltip: Title → Purpose → Cách cấu hình → Ví dụ → Lưu ý.</summary>
    private static UIElement BuildContent(FieldHelpTopic topic)
    {
        var root = new StackPanel { MaxWidth = 430 };

        root.Children.Add(MakeText(topic.Title, 13, FontWeights.SemiBold, "AppAccentDarkBrush",
                                   margin: new Thickness(0, 0, 0, 6)));
        root.Children.Add(MakeText(topic.Purpose, 12, FontWeights.Normal, "AppTextSecondaryBrush",
                                   margin: new Thickness(0, 0, 0, topic.HowTo.Count > 0 ? 8 : 0)));

        if (topic.HowTo.Count > 0)
        {
            root.Children.Add(MakeText("Cách cấu hình đúng:", 11, FontWeights.SemiBold, "AppTextBrush",
                                       margin: new Thickness(0, 0, 0, 3)));
            foreach (var line in topic.HowTo)
                root.Children.Add(MakeText("•  " + line, 11.5, FontWeights.Normal, "AppTextSecondaryBrush",
                                           margin: new Thickness(4, 1, 0, 1)));
        }

        if (topic.HasExample)
        {
            var example = new TextBlock
            {
                FontSize     = 11.5,
                TextWrapping = TextWrapping.Wrap,
                Margin       = new Thickness(0, 8, 0, 0),
            };
            var label = new Run("Ví dụ:  ") { FontWeight = FontWeights.SemiBold };
            label.SetResourceReference(TextElement.ForegroundProperty, "AppTextBrush");
            var value = new Run(topic.Example!) { FontFamily = new FontFamily("Consolas") };
            value.SetResourceReference(TextElement.ForegroundProperty, "AppSuccessDarkBrush");
            example.Inlines.Add(label);
            example.Inlines.Add(value);
            root.Children.Add(example);
        }

        if (topic.HasPitfalls)
        {
            root.Children.Add(MakeText("⚠ Lỗi thường gặp:", 11, FontWeights.SemiBold, "AppWarningDarkBrush",
                                       margin: new Thickness(0, 8, 0, 3)));
            foreach (var line in topic.Pitfalls!)
                root.Children.Add(MakeText("•  " + line, 11.5, FontWeights.Normal, "AppWarningDarkBrush",
                                           margin: new Thickness(4, 1, 0, 1)));
        }

        return root;
    }

    /// <summary>Tạo TextBlock wrap sẵn, màu lấy theo DynamicResource để ăn theme (ADR-031).</summary>
    private static TextBlock MakeText(string text, double size, FontWeight weight, string brushKey, Thickness margin)
    {
        var tb = new TextBlock
        {
            Text         = text,
            FontSize     = size,
            FontWeight   = weight,
            TextWrapping = TextWrapping.Wrap,
            Margin       = margin,
        };
        tb.SetResourceReference(TextBlock.ForegroundProperty, brushKey);
        return tb;
    }
}
