using System.Windows;

namespace ModenTheme
{
    class Ext
    {
        // Image
        public static readonly DependencyProperty ImageProperty = DependencyProperty.RegisterAttached(
            "Image", typeof(FrameworkElement), typeof(Ext), new FrameworkPropertyMetadata(null));

        public static FrameworkElement GetImage(DependencyObject obj) => (FrameworkElement)obj.GetValue(ImageProperty);
        public static void SetImage(DependencyObject obj, FrameworkElement value) => obj.SetValue(ImageProperty, value);

        // DisabledImage
        public static readonly DependencyProperty DisabledImageProperty = DependencyProperty.RegisterAttached(
            "DisabledImage", typeof(FrameworkElement), typeof(Ext), new FrameworkPropertyMetadata(null));

        public static FrameworkElement GetDisabledImage(DependencyObject obj) => (FrameworkElement)obj.GetValue(DisabledImageProperty);
        public static void SetDisabledImage(DependencyObject obj, FrameworkElement value) => obj.SetValue(DisabledImageProperty, value);

        // IsToolWindow
        public static readonly DependencyProperty IsToolWindowProperty = DependencyProperty.RegisterAttached(
            "IsToolWindow", typeof(bool), typeof(Ext), new FrameworkPropertyMetadata(false));

        public static bool GetIsToolWindow(DependencyObject obj) => (bool)obj.GetValue(IsToolWindowProperty);
        public static void SetIsToolWindow(DependencyObject obj, bool value) => obj.SetValue(IsToolWindowProperty, value);
    }
}
