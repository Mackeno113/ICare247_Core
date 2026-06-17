// File    : GridLayoutBehavior.cs
// Module  : Infrastructure / Behaviors
// Layer   : Presentation
// Purpose : Tự lưu & phục hồi layout lưới DevExpress (độ rộng/thứ tự/sort cột) ra FILE LOCAL,
//           KHÔNG đụng DB. Gắn 1 lần qua implicit style của dxg:TableView (Themes/Controls.xaml)
//           nên phủ mọi GridControl toàn app mà không phải sửa từng màn.
//
// Cơ chế  : - Mỗi lưới có 1 "profile key" tự sinh = {TênUserControl} + chữ ký cột (FieldName) → 1 file XML.
//           - Loaded  → nếu có file thì RestoreLayoutFromXml (áp lại độ rộng người dùng đã chỉnh).
//           - Unloaded / thoát app → SaveLayoutToXml.
//           - File tại: %LOCALAPPDATA%\ICare247\ConfigStudio\GridLayouts\*.xml
//
// An toàn : Mọi lỗi I/O đều nuốt (Debug.WriteLine) — persistence layout KHÔNG bao giờ được làm crash app.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using DevExpress.Xpf.Grid;

namespace ConfigStudio.WPF.UI.Infrastructure.Behaviors;

/// <summary>
/// Attached behavior gắn lên <see cref="TableView"/>: tự lưu/phục hồi bố cục cột của
/// <see cref="GridControl"/> chủ ra file XML cục bộ (per-máy, không vào DB).
/// </summary>
public static class GridLayoutBehavior
{
    // ── Attached property bật/tắt ─────────────────────────────────────────────
    /// <summary>Bật persistence cho TableView (đặt qua implicit style).</summary>
    public static readonly DependencyProperty PersistProperty =
        DependencyProperty.RegisterAttached(
            "Persist", typeof(bool), typeof(GridLayoutBehavior),
            new PropertyMetadata(false, OnPersistChanged));

    public static void SetPersist(DependencyObject d, bool value) => d.SetValue(PersistProperty, value);
    public static bool GetPersist(DependencyObject d) => (bool)d.GetValue(PersistProperty);

    // Lưu profile key đã tính cho mỗi view (tránh tính lại + dùng khi Unloaded).
    private static readonly DependencyProperty ProfileKeyProperty =
        DependencyProperty.RegisterAttached(
            "ProfileKey", typeof(string), typeof(GridLayoutBehavior), new PropertyMetadata(null));

    // Các view đang sống — flush khi thoát app (Unloaded có thể không kịp khi đóng cửa sổ).
    private static readonly HashSet<TableView> LiveViews = new();
    private static bool _exitHooked;

    private static void OnPersistChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not TableView view) return;

        if (e.NewValue is true)
        {
            view.Loaded += OnViewLoaded;
            view.Unloaded += OnViewUnloaded;
        }
        else
        {
            view.Loaded -= OnViewLoaded;
            view.Unloaded -= OnViewUnloaded;
        }
    }

    private static void OnViewLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is not TableView view) return;
        if (view.DataControl is not GridControl grid) return;

        try
        {
            var key = view.GetValue(ProfileKeyProperty) as string;
            if (string.IsNullOrEmpty(key))
            {
                key = BuildProfileKey(grid);
                view.SetValue(ProfileKeyProperty, key);
            }

            HookAppExitOnce();
            LiveViews.Add(view);

            var path = PathFor(key!);
            if (File.Exists(path))
                grid.RestoreLayoutFromXml(path);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[GridLayout] Restore failed: {ex.Message}");
            TryDeleteCorrupt(view);
        }
    }

    private static void OnViewUnloaded(object sender, RoutedEventArgs e)
    {
        if (sender is TableView view)
        {
            SaveView(view);
            LiveViews.Remove(view);
        }
    }

    private static void SaveView(TableView view)
    {
        if (view.DataControl is not GridControl grid) return;
        var key = view.GetValue(ProfileKeyProperty) as string;
        if (string.IsNullOrEmpty(key)) return;

        try
        {
            var dir = LayoutDir();
            Directory.CreateDirectory(dir);
            grid.SaveLayoutToXml(PathFor(key!));
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[GridLayout] Save failed: {ex.Message}");
        }
    }

    private static void HookAppExitOnce()
    {
        if (_exitHooked || Application.Current is null) return;
        _exitHooked = true;
        Application.Current.Exit += (_, _) =>
        {
            foreach (var v in LiveViews.ToArray())
                SaveView(v);
        };
    }

    private static void TryDeleteCorrupt(TableView view)
    {
        try
        {
            var key = view.GetValue(ProfileKeyProperty) as string;
            if (!string.IsNullOrEmpty(key))
            {
                var path = PathFor(key!);
                if (File.Exists(path)) File.Delete(path);
            }
        }
        catch { /* bỏ qua */ }
    }

    // ── Sinh key ổn định cho 1 lưới ───────────────────────────────────────────
    // Key = {TênUserControl chủ} + chữ ký cột. Hai lưới khác bộ cột (vd list vs editor)
    // trong cùng màn → khác file. Đổi tên/đổi cột → key đổi → layout reset (chấp nhận được).
    private static string BuildProfileKey(GridControl grid)
    {
        var owner = FindOwnerName(grid);
        var colSig = string.Join(",", grid.Columns.Select(c => c.FieldName ?? string.Empty));
        var disamb = !string.IsNullOrEmpty(grid.Name) ? "n:" + grid.Name : "c:" + colSig;
        return owner + "::" + disamb;
    }

    private static string FindOwnerName(DependencyObject start)
    {
        DependencyObject? cur = start;
        while (cur is not null)
        {
            if (cur is FrameworkElement fe && fe is System.Windows.Controls.UserControl)
                return cur.GetType().Name;
            cur = VisualTreeHelper.GetParent(cur)
                  ?? (cur is FrameworkElement f ? f.Parent : null);
        }
        return start.GetType().Name;
    }

    // ── Đường dẫn file ────────────────────────────────────────────────────────
    private static string LayoutDir() => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ICare247", "ConfigStudio", "GridLayouts");

    private static string PathFor(string key)
    {
        // Tên file = phần đọc-được (tên UserControl) + hash ổn định của key đầy đủ.
        var readable = key.Split(new[] { "::" }, StringSplitOptions.None)[0];
        var safe = SanitizeFileName(readable);
        return Path.Combine(LayoutDir(), $"{safe}_{Fnv1a(key):x8}.xml");
    }

    private static string SanitizeFileName(string s)
    {
        var sb = new StringBuilder(s.Length);
        foreach (var ch in s)
            sb.Append(Array.IndexOf(Path.GetInvalidFileNameChars(), ch) >= 0 ? '_' : ch);
        return sb.Length == 0 ? "grid" : sb.ToString();
    }

    // FNV-1a 32-bit — hash ỔN ĐỊNH qua các lần chạy (KHÔNG dùng string.GetHashCode vì bị randomize).
    private static uint Fnv1a(string s)
    {
        const uint offset = 2166136261, prime = 16777619;
        uint hash = offset;
        foreach (var ch in s)
        {
            hash ^= ch;
            hash *= prime;
        }
        return hash;
    }
}
