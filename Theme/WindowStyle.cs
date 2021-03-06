using System;
using System.Windows.Input;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using MCUTerm.Controls;

namespace ModenTheme
{
    internal static class LocalExtensions
    {
        public static void ForWindowFromTemplate(this object templateFrameworkElement, Action<Window> action)
        {
            Window window = ((FrameworkElement)templateFrameworkElement).TemplatedParent as Window;
            if (window != null) action(window);
        }

        public static IntPtr GetWindowHandle(this Window window)
        {
            WindowInteropHelper helper = new WindowInteropHelper(window);
            return helper.Handle;
        }
    }

    public partial class WindowStyle
    {
        protected void IconMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount > 1)
                sender.ForWindowFromTemplate(w => SystemCommands.CloseWindow(w));
        }

        protected void IconMouseUp(object sender, MouseButtonEventArgs e)
        {
            var element = sender as FrameworkElement;
            var point = element.PointToScreen(new Point(element.ActualWidth / 2, element.ActualHeight));
            sender.ForWindowFromTemplate(w => SystemCommands.ShowSystemMenu(w, point));
        }

        protected void WindowLoaded(object sender, RoutedEventArgs e)
        {
            ((Window)sender).StateChanged += WindowStateChanged;
        }

        protected void WindowStateChanged(object sender, EventArgs e)
        {
            var w = ((Window)sender);
            var handle = w.GetWindowHandle();
            var containerBorder = (Border)w.Template.FindName("WindowBorder", w);

            if (w.WindowState == WindowState.Maximized)
            {
                double dpiScaleX = VisualTreeHelper.GetDpi(w).DpiScaleX;
                double dpiScaleY = VisualTreeHelper.GetDpi(w).DpiScaleY;
                // Make sure window doesn't overlap with the taskbar.
                var screen = System.Windows.Forms.Screen.FromHandle(handle);
                containerBorder.Margin = new Thickness(
                    DPIHelper.RoundByPixelBound(7 - 1, dpiScaleX),
                    DPIHelper.RoundByPixelBound(7 - 1, dpiScaleY),
                    DPIHelper.RoundByPixelBound((screen.Bounds.Width - screen.WorkingArea.Width) / dpiScaleX + 7 - 1, dpiScaleX),
                    DPIHelper.RoundByPixelBound((screen.Bounds.Height - screen.WorkingArea.Height) / dpiScaleY + 7 - 1, dpiScaleY));
            }
            else
            {
                containerBorder.Margin = new Thickness(0, 0, 0, 0);
            }
        }

        protected void CloseButtonClick(object sender, RoutedEventArgs e)
        {
            sender.ForWindowFromTemplate(w => SystemCommands.CloseWindow(w));
        }

        protected void MinimizeButtonClick(object sender, RoutedEventArgs e)
        {
            sender.ForWindowFromTemplate(w => SystemCommands.MinimizeWindow(w));
        }

        protected void MaximizeButtonClick(object sender, RoutedEventArgs e)
        {
            sender.ForWindowFromTemplate(w => SystemCommands.MaximizeWindow(w));
        }

        protected void RestoreButtonClick(object sender, RoutedEventArgs e)
        {
            sender.ForWindowFromTemplate(w => SystemCommands.RestoreWindow(w));
        }
    }
}