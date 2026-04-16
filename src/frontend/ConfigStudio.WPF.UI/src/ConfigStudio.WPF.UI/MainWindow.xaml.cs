// File    : MainWindow.xaml.cs
// Module  : Shell
// Layer   : Presentation
// Purpose : Code-behind shell window + DataTemplateSelector cho custom sidebar.
//           Hook WM_GETMINMAXINFO để maximize không che taskbar (WindowStyle=None).

using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using ConfigStudio.WPF.UI.Models;

namespace ConfigStudio.WPF.UI;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    // ── Hook Win32 để maximize không che taskbar ──────────────────────────────

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        // Đăng ký WndProc hook — cần thiết vì WindowStyle=None bỏ qua work area mặc định
        var source = HwndSource.FromHwnd(new WindowInteropHelper(this).Handle);
        source?.AddHook(WndProc);
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        // WM_GETMINMAXINFO (0x0024): Windows hỏi kích thước tối đa khi maximize
        if (msg == 0x0024)
        {
            WmGetMinMaxInfo(hwnd, lParam);
            handled = true;
        }
        return IntPtr.Zero;
    }

    /// <summary>
    /// Giới hạn kích thước maximize = work area (màn hình trừ taskbar).
    /// Không dùng: screen.WorkingArea (không hỗ trợ multi-monitor đúng cách).
    /// </summary>
    private static void WmGetMinMaxInfo(IntPtr hwnd, IntPtr lParam)
    {
        var mmi = Marshal.PtrToStructure<MINMAXINFO>(lParam);

        // Tìm monitor chứa cửa sổ (hỗ trợ multi-monitor)
        var monitor = NativeMethods.MonitorFromWindow(hwnd, NativeMethods.MONITOR_DEFAULTTONEAREST);
        if (monitor != IntPtr.Zero)
        {
            var info = new NativeMethods.MONITORINFO();
            info.cbSize = Marshal.SizeOf<NativeMethods.MONITORINFO>();
            NativeMethods.GetMonitorInfo(monitor, ref info);

            var workArea    = info.rcWork;    // vùng làm việc (trừ taskbar)
            var monitorArea = info.rcMonitor; // toàn màn hình

            // ptMaxPosition: vị trí góc trên trái khi maximize (tính tương đối với monitor)
            mmi.ptMaxPosition.x = Math.Abs(workArea.left   - monitorArea.left);
            mmi.ptMaxPosition.y = Math.Abs(workArea.top    - monitorArea.top);
            // ptMaxSize: kích thước tối đa = work area (không che taskbar)
            mmi.ptMaxSize.x     = Math.Abs(workArea.right  - workArea.left);
            mmi.ptMaxSize.y     = Math.Abs(workArea.bottom - workArea.top);
            // ptMinTrackSize: kích thước tối thiểu khi kéo resize
            mmi.ptMinTrackSize.x = 1024;
            mmi.ptMinTrackSize.y = 640;
        }

        Marshal.StructureToPtr(mmi, lParam, true);
    }

    // ── Drag + Double-click maximize ─────────────────────────────────────────

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Left) return;

        if (e.ClickCount == 2)
        {
            WindowState = WindowState == WindowState.Maximized
                ? WindowState.Normal
                : WindowState.Maximized;
            return;
        }

        // Chỉ cho phép drag khi không maximize
        if (WindowState == WindowState.Normal)
            DragMove();
    }

    // ── Win32 structs + P/Invoke ─────────────────────────────────────────────

    [StructLayout(LayoutKind.Sequential)]
    private struct MINMAXINFO
    {
        public POINT ptReserved;
        public POINT ptMaxSize;
        public POINT ptMaxPosition;
        public POINT ptMinTrackSize;
        public POINT ptMaxTrackSize;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT { public int x, y; }

    private static class NativeMethods
    {
        public const uint MONITOR_DEFAULTTONEAREST = 0x00000002;

        [DllImport("user32.dll")]
        public static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct MONITORINFO
        {
            public int   cbSize;
            public RECT  rcMonitor;
            public RECT  rcWork;
            public uint  dwFlags;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int left, top, right, bottom;
        }
    }
}

/// <summary>
/// Chọn DataTemplate phù hợp cho từng loại SidebarEntry:
/// header (section label), divider (đường kẻ ngang), item (nút điều hướng).
/// </summary>
public sealed class SidebarTemplateSelector : DataTemplateSelector
{
    public DataTemplate? HeaderTemplate  { get; set; }
    public DataTemplate? DividerTemplate { get; set; }
    public DataTemplate? ItemTemplate    { get; set; }

    public override DataTemplate? SelectTemplate(object item, DependencyObject container)
    {
        if (item is SidebarEntry entry)
        {
            if (entry.IsDivider) return DividerTemplate;
            if (entry.IsHeader)  return HeaderTemplate;
            return ItemTemplate;
        }
        return base.SelectTemplate(item, container);
    }
}
