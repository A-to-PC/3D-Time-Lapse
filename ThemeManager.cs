using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using Microsoft.Win32;

namespace TimeLapse3D;

public static class ThemeManager
{
    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

    private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
    private const int DWMWA_USE_IMMERSIVE_DARK_MODE_OLD = 19;

    public static bool IsSystemDarkTheme()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            var value = key?.GetValue("AppsUseLightTheme");
            return value is int lightThemeFlag && lightThemeFlag == 0;
        }
        catch
        {
            return false;
        }
    }

    public static void ApplyResources()
    {
        var dark = IsSystemDarkTheme();
        var res = Application.Current.Resources;

        if (dark)
        {
            res["WindowBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(0x20, 0x20, 0x20));
            res["ControlBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(0x2D, 0x2D, 0x2D));
            res["ControlForegroundBrush"] = new SolidColorBrush(Colors.WhiteSmoke);
            res["ControlBorderBrush"] = new SolidColorBrush(Color.FromRgb(0x55, 0x55, 0x55));
            res["SecondaryForegroundBrush"] = new SolidColorBrush(Color.FromRgb(0xAA, 0xAA, 0xAA));
        }
        else
        {
            res["WindowBackgroundBrush"] = new SolidColorBrush(Colors.White);
            res["ControlBackgroundBrush"] = new SolidColorBrush(Colors.White);
            res["ControlForegroundBrush"] = new SolidColorBrush(Colors.Black);
            res["ControlBorderBrush"] = new SolidColorBrush(Color.FromRgb(0xAC, 0xAC, 0xAC));
            res["SecondaryForegroundBrush"] = new SolidColorBrush(Colors.Gray);
        }
    }

    public static void ApplyTitleBar(Window window)
    {
        var hwnd = new WindowInteropHelper(window).Handle;
        if (hwnd == IntPtr.Zero) return;

        var dark = IsSystemDarkTheme() ? 1 : 0;
        if (DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref dark, sizeof(int)) != 0)
        {
            DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE_OLD, ref dark, sizeof(int));
        }
    }

    /// <summary>Applies resources + title bar now, and keeps both in sync if the user flips Windows theme while the app is open.</summary>
    public static void Track(Window window)
    {
        ApplyResources();
        window.SourceInitialized += (_, _) => ApplyTitleBar(window);

        SystemEvents.UserPreferenceChanged += (_, e) =>
        {
            if (e.Category != UserPreferenceCategory.General) return;
            window.Dispatcher.Invoke(() =>
            {
                ApplyResources();
                ApplyTitleBar(window);
            });
        };
    }
}
