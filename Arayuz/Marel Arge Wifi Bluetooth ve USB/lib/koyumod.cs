using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;


namespace marel_arge
{

    public static class Koyumod_UI
    {

        const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
        enum DWM_SYSTEMBACKDROP_TYPE
        {
            Mica = 2, // Mica
            Acrylic = 3, // Acrylic
            Tabbed = 4 // Tabbed
        }
        
        [Flags]
        public enum DWMWINDOWATTRIBUTE
        {
            DWMWA_USE_IMMERSIVE_DARK_MODE_win10 = 19,
            DWMWA_USE_IMMERSIVE_DARK_MODE = 20,
            DWMWA_SYSTEMBACKDROP_TYPE = 38
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MARGINS
        {
            public int cxLeftWidth;      // width of left border that retains its size
            public int cxRightWidth;     // width of right border that retains its size
            public int cyTopHeight;      // height of top border that retains its size
            public int cyBottomHeight;   // height of bottom border that retains its size
        };

        [DllImport("UXTheme.dll", SetLastError = true, EntryPoint = "#138")]
        public static extern bool ShouldSystemUseDarkMode();

        [DllImport("DwmApi.dll")]
        static extern int DwmExtendFrameIntoClientArea(IntPtr hwnd, ref MARGINS pMarInset);

        [DllImport("dwmapi.dll")]
        static extern int DwmSetWindowAttribute(IntPtr hwnd, DWMWINDOWATTRIBUTE dwAttribute, ref int pvAttribute, int cbAttribute);

        [DllImport("dwmapi.dll", PreserveSig = true)]
        public static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        public static int ExtendFrame(IntPtr hwnd, MARGINS margins)
        => DwmExtendFrameIntoClientArea(hwnd, ref margins);

        public static int SetWindowAttribute(IntPtr hwnd, DWMWINDOWATTRIBUTE attribute, int parameter)
        => DwmSetWindowAttribute(hwnd, attribute, ref parameter, Marshal.SizeOf<int>());

        public static void cerceveyi_koyumod_yap(Window window, bool enableDarkMode)
        {
           var hwnd = new WindowInteropHelper(window).Handle;
           int isDarkMode = enableDarkMode ? 1 : 0;
           DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref isDarkMode, sizeof(int));
        }


        public static void cerceveyi_ayarla(Window window)
        {
            IntPtr mainWindowPtr = new WindowInteropHelper(window).Handle;
            HwndSource mainWindowSrc = HwndSource.FromHwnd(mainWindowPtr);
            mainWindowSrc.CompositionTarget.BackgroundColor = Color.FromArgb(0, 0, 0, 0);

            MARGINS margins = new MARGINS();
            margins.cxLeftWidth = -1;
            margins.cxRightWidth = -1;
            margins.cyTopHeight = -1;
            margins.cyBottomHeight = -1;

            ExtendFrame(mainWindowSrc.Handle, margins);
        }

        public static void karanlikmodu_yenile(Window window)
        {
            int flag = ShouldSystemUseDarkMode() ? 1 : 0;
            var hwnd = new WindowInteropHelper(window).Handle;
            SetWindowAttribute(hwnd, DWMWINDOWATTRIBUTE.DWMWA_USE_IMMERSIVE_DARK_MODE, flag);
            //if (flag == 1) { MessageBox.Show( "koyu mod" ); }else { MessageBox.Show("acık mod"); }
        }

        public static void pencereyi_ayarla(Window window)
        {
            var hwnd = new WindowInteropHelper(window).Handle;
            SetWindowAttribute(hwnd, DWMWINDOWATTRIBUTE.DWMWA_SYSTEMBACKDROP_TYPE, (int)DWM_SYSTEMBACKDROP_TYPE.Mica);
        }

        public static void temayı_degistir(Window window)
        {
            try
            {
                if (IsWindows11())
                {
                    if (window.IsLoaded)
                    {
                        cerceveyi_ayarla(window);
                        karanlikmodu_yenile(window);
                        //ThemeManager.Current.ActualApplicationThemeChanged += (_, _) => RefreshDarkMode();
                        pencereyi_ayarla(window);
                        arayuz_birlesenlerini_guncelle(window);
                    }
                }
                else
                {
                    if (ShouldSystemUseDarkMode())
                    {
                        arayuz_birlesenlerini_guncelle(window);
                        var hwnd = new WindowInteropHelper(window).Handle;
                        SetWindowAttribute(hwnd, DWMWINDOWATTRIBUTE.DWMWA_USE_IMMERSIVE_DARK_MODE, 1);
                        cerceveyi_ayarla(window);
                    }
                    else
                    {
                        arayuz_birlesenlerini_guncelle(window);
                        cerceveyi_ayarla(window);
                    }
                }
            }
            catch{}
        }


        private static void arayuz_birlesenlerini_guncelle(Window window)
        {
            bool isDarkMode = Koyumod_UI.ShouldSystemUseDarkMode();

            Brush backgroundColor = isDarkMode ? new SolidColorBrush(Colors.Black) : new SolidColorBrush(Colors.White);
            Brush foregroundColor = isDarkMode ? new SolidColorBrush(Colors.White) : new SolidColorBrush(Colors.Black);
       
            foreach (var control in FindVisualChildren<Label>(window))
            {
                control.Foreground = foregroundColor;
            }

            foreach (var control in FindVisualChildren<TextBox>(window))
            {
                control.Background = backgroundColor;
                control.Foreground = foregroundColor;
            }

            foreach (var control in FindVisualChildren<Button>(window))
            {
                control.Background = backgroundColor;
                control.Foreground = foregroundColor;
                control.BorderBrush = foregroundColor;
            }

            foreach (var control in FindVisualChildren<CheckBox>(window))
            {
                control.Background = backgroundColor;
                control.Foreground = foregroundColor;
                control.BorderBrush = foregroundColor;
            }

            foreach (var control in FindVisualChildren<Border>(window))
            {
                control.Background = new SolidColorBrush(Colors.Transparent);
            }
        }

        static bool IsWindows11()
        {
            try
            {
                // Registry'den CurrentBuild değerini oku
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion"))
                {
                    if (key != null)
                    {
                        string currentBuild = key.GetValue("CurrentBuild") as string;

                        if (int.TryParse(currentBuild, out int buildNumber))
                        {
                            // Windows 11'in minimum build numarası 22000'dir.
                            if (buildNumber < 22000)
                            {
                                return false; // Windows 11'in altında
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Bir hata oluştu: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return true; // Windows 11 veya üstü
        }

        public static IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj != null)
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
                    if (child != null && child is T)
                    {
                        yield return (T)child;
                    }

                    foreach (T childOfChild in FindVisualChildren<T>(child))
                    {
                        yield return childOfChild;
                    }
                }
            }
        }
    }
}


