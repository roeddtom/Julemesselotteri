using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace Julemesselotteri.App;

public sealed record DisplayInfo(string Device, string Label, bool Primary, int Left, int Top, int Width, int Height)
{
    public System.Windows.Rect WorkArea { get; init; } = new(Left, Top, Width, Height);
}

public static class DisplayManager
{
    [StructLayout(LayoutKind.Sequential)]
    private struct Rect { public int Left, Top, Right, Bottom; }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct MonitorInfo
    {
        public int Size;
        public Rect Monitor, Work;
        public uint Flags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string Device;
    }

    private delegate bool MonitorCallback(nint monitor, nint hdc, ref Rect rectangle, nint data);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EnumDisplayMonitors(nint hdc, nint clip, MonitorCallback callback, nint data);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetMonitorInfo(nint monitor, ref MonitorInfo info);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetWindowPos(nint hwnd, nint after, int x, int y, int width, int height, uint flags);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetWindowRect(nint hwnd, out Rect rectangle);

    public static List<DisplayInfo> GetDisplays()
    {
        var displays = new List<DisplayInfo>();
        EnumDisplayMonitors(0, 0, (nint monitor, nint hdc, ref Rect rectangle, nint data) =>
        {
            var info = new MonitorInfo { Size = Marshal.SizeOf<MonitorInfo>(), Device = "" };
            if (GetMonitorInfo(monitor, ref info))
                displays.Add(new DisplayInfo(info.Device,
                    $"Skjerm {displays.Count + 1}{((info.Flags & 1) != 0 ? " (hovedskjerm)" : "")} · " +
                    $"{info.Monitor.Right - info.Monitor.Left} × {info.Monitor.Bottom - info.Monitor.Top}",
                    (info.Flags & 1) != 0, info.Monitor.Left, info.Monitor.Top,
                    info.Monitor.Right - info.Monitor.Left, info.Monitor.Bottom - info.Monitor.Top)
                {
                    WorkArea = new System.Windows.Rect(info.Work.Left, info.Work.Top,
                        info.Work.Right - info.Work.Left, info.Work.Bottom - info.Work.Top)
                });
            return true;
        }, 0);
        return displays;
    }

    public static void Place(Window window, string device) => Place(window, device, GetDisplays());

    internal static IReadOnlyList<DisplayInfo> AudienceDisplays(IReadOnlyList<DisplayInfo> displays) =>
        displays.Any(d => !d.Primary) ? displays.Where(d => !d.Primary).ToArray() : displays;

    internal static DisplayInfo SelectAudienceDisplay(string device, IReadOnlyList<DisplayInfo> displays)
    {
        var candidates = AudienceDisplays(displays);
        return candidates.FirstOrDefault(d => d.Device == device) ?? candidates.FirstOrDefault()
            ?? throw new InvalidOperationException("Fant ingen tilgjengelig publikumsskjerm.");
    }

    public static void PlaceOperator(Window window) => PlaceOperator(window, GetDisplays());

    internal static void PlaceOperator(Window window, IReadOnlyList<DisplayInfo> displays)
    {
        var display = displays.FirstOrDefault(d => d.Primary) ?? displays.FirstOrDefault()
            ?? throw new InvalidOperationException("Fant ingen tilgjengelig operatørskjerm.");
        window.WindowStartupLocation = WindowStartupLocation.Manual;
        var handle = new WindowInteropHelper(window).EnsureHandle();
        if (!GetWindowRect(handle, out var bounds))
            throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
        var monitor = new System.Windows.Rect(display.Left, display.Top, display.Width, display.Height);
        if (window.IsVisible && monitor.Contains(new Point(
            (bounds.Left + bounds.Right) / 2.0, (bounds.Top + bounds.Bottom) / 2.0))) return;
        var maximized = window.WindowState == WindowState.Maximized;
        window.WindowState = WindowState.Normal;
        var area = display.WorkArea;
        var width = Math.Min(bounds.Right - bounds.Left, (int)area.Width);
        var height = Math.Min(bounds.Bottom - bounds.Top, (int)area.Height);
        if (!SetWindowPos(handle, 0,
            (int)area.Left + ((int)area.Width - width) / 2, (int)area.Top + ((int)area.Height - height) / 2,
            width, height, 0x0010 | 0x0004))
            throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
        if (maximized) window.WindowState = WindowState.Maximized;
    }

    internal static void Place(Window window, string device, IReadOnlyList<DisplayInfo> displays)
    {
        var display = SelectAudienceDisplay(device, displays);
        window.WindowStartupLocation = WindowStartupLocation.Manual;
        if (displays.Count == 1)
        {
            window.Topmost = false;
            if (window.WindowStyle != WindowStyle.SingleBorderWindow)
            {
                window.WindowState = WindowState.Normal;
                window.WindowStyle = WindowStyle.SingleBorderWindow;
                window.ResizeMode = ResizeMode.CanResize;
                var area = SystemParameters.WorkArea;
                window.Width = Math.Min(960, area.Width * 0.8);
                window.Height = Math.Min(600, area.Height * 0.8);
                window.Left = area.Left + (area.Width - window.Width) / 2;
                window.Top = area.Top + (area.Height - window.Height) / 2;
            }
            return;
        }
        window.WindowState = WindowState.Normal;
        window.WindowStyle = WindowStyle.None;
        window.ResizeMode = ResizeMode.NoResize;
        // Cover independent slide shows on the projector without trapping the operator on a single screen.
        window.Topmost = !display.Primary;
        if (!SetWindowPos(new WindowInteropHelper(window).EnsureHandle(), window.Topmost ? -1 : 0,
                display.Left, display.Top, display.Width, display.Height, 0x0010))
            throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
    }
}
