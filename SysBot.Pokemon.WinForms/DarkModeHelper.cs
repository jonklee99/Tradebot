using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace SysBot.Pokemon.WinForms
{
    public static class DarkModeHelper
    {
        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
        private static extern int SetWindowTheme(IntPtr hWnd, string pszSubAppName, string? pszSubIdList);

        private const int DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1 = 19;
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
        private const int DWMWA_CAPTION_COLOR = 35;
        private const int DWMWA_BORDER_COLOR = 34;

        public static bool SetDarkMode(IntPtr handle)
        {
            if (IsWindows10OrGreater())
            {
                var attribute = DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1;
                if (IsWindows10OrGreater(18985))
                {
                    attribute = DWMWA_USE_IMMERSIVE_DARK_MODE;
                }

                int useImmersiveDarkMode = 1;
                var result = DwmSetWindowAttribute(handle, attribute, ref useImmersiveDarkMode, sizeof(int));
                
                if (IsWindows11OrGreater())
                {
                    int darkColor = ColorTranslator.ToWin32(System.Drawing.Color.FromArgb(23, 29, 37));
                    DwmSetWindowAttribute(handle, DWMWA_CAPTION_COLOR, ref darkColor, sizeof(int));
                    
                    int borderColor = ColorTranslator.ToWin32(System.Drawing.Color.FromArgb(32, 38, 48));
                    DwmSetWindowAttribute(handle, DWMWA_BORDER_COLOR, ref borderColor, sizeof(int));
                }

                return result == 0;
            }
            return false;
        }

        private static bool IsWindows10OrGreater(int build = -1)
        {
            var version = Environment.OSVersion.Version;
            var isWin10 = version.Major >= 10;
            
            if (build == -1)
                return isWin10;
                
            return isWin10 && version.Build >= build;
        }

        private static bool IsWindows11OrGreater()
        {
            var version = Environment.OSVersion.Version;
            return version.Major >= 10 && version.Build >= 22000;
        }

        public static void ApplyDarkModeToControl(Control control)
        {
            SetWindowTheme(control.Handle, "DarkMode_Explorer", null);
        }
    }
}