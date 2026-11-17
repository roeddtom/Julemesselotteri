using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace Julemesselotteri.App;

public sealed record DisplayInfo(string Device, string Label, bool Primary, int Left, int Top, int Width, int Height);

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
                    info.Monitor.Right - info.Monitor.Left, info.Monitor.Bottom - info.Monitor.Top));
            return true;
        }, 0);
        return displays;
    }

    public static void Place(Window window, string device) => Place(window, device, GetDisplays());

    internal static void Place(Window window, string device, IReadOnlyList<DisplayInfo> displays)
    {
        var display = displays.FirstOrDefault(d => d.Device == device)
            ?? displays.FirstOrDefault(d => !d.Primary) ?? displays.FirstOrDefault();
        if (display is null) throw new InvalidOperationException("Fant ingen tilgjengelig publikumsskjerm.");
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
        if (!SetWindowPos(new WindowInteropHelper(window).Handle, window.Topmost ? -1 : 0,
                display.Left, display.Top, display.Width, display.Height, 0x0010 | 0x0040))
            throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
    }
}
