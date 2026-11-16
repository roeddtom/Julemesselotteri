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

    public static void Place(Window window, string device)
    {
        var displays = GetDisplays();
        var display = displays.FirstOrDefault(d => d.Device == device)
            ?? displays.FirstOrDefault(d => !d.Primary) ?? displays.FirstOrDefault();
        if (display is null) throw new InvalidOperationException("Fant ingen tilgjengelig publikumsskjerm.");
        if (!SetWindowPos(new WindowInteropHelper(window).Handle, 0,
                display.Left, display.Top, display.Width, display.Height, 0x0010 | 0x0040))
            throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
    }
}
