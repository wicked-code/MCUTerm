using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.IO;
using Microsoft.Win32;

namespace MCUTerm
{
    public class BaudRateRule : ValidationRule
    {
        public override ValidationResult Validate(object value, System.Globalization.CultureInfo cultureInfo)
        {
            int baud = 0;
            string str = "";

            try
            {
                str = (string)value;
                if (str.Length > 0)
                    baud = int.Parse(str);
            }
            catch
            {
                return new ValidationResult(false, "Not a number");
            }

            if (str.Length > 0 && (baud < 300 || baud > 1000000))
                return new ValidationResult(false, "Invalid baud rate");
            else
                return new ValidationResult(true, null);
        }
    }

    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public sealed partial class SettingsWindow : Window
    {
        public string BaudRate { get; set; }

        public SettingsWindow()
        {
            InitializeComponent();
            this.DataContext = this;
        }

        private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Border control = (Border)sender;
            control.SetValue(Border.PaddingProperty, new Thickness(3));
        }

        private void Border_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Border control = (Border)sender;
            control.ClearValue(Border.PaddingProperty);

            ProcessAction((FrameworkElement)sender);
        }

        private void NewBaudBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                AddNewBaud();
        }

        private void ListBoxItem_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter || e.Key == Key.Space)
                ProcessAction((FrameworkElement)sender);
        }

        private void AddNewBaud()
        {
            // check valid
            if (BaudRate.Length == 0)
                return;

            Properties.Settings.Default.BaudList.Add(BaudRate);
            NewBaudBox.Clear();
        }

        private void ProcessAction(FrameworkElement element)
        {
            while (element != null & (element is ListBoxItem) == false)
                element = (FrameworkElement)VisualTreeHelper.GetParent(element);

            if (element == null)
            {
                System.Diagnostics.Debug.Assert(false, "Visual Tree is invalid.");
                return;
            }

            if (element.Name == AddBaudItem.Name)
            {
                AddNewBaud();
                return;
            }

            ListBoxItem listBoxItem = (ListBoxItem)element;
            Properties.Settings.Default.BaudList.Remove(listBoxItem.Content.ToString());
        }

        private void Button_OK_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void SelectLog_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.AddExtension = true;
            saveFileDialog.CheckPathExists = true;
            saveFileDialog.DefaultExt = ".log";
            saveFileDialog.FileName = "MCUTerm.log";
            saveFileDialog.Filter = "Log Files (*.log)|*.log|All Files (*.*)|*.*";
            saveFileDialog.OverwritePrompt = false;
            saveFileDialog.Title = "Select Log File";
            saveFileDialog.ValidateNames = true;
            saveFileDialog.InitialDirectory = System.AppDomain.CurrentDomain.BaseDirectory;

            if (saveFileDialog.ShowDialog() == false)
                return;

            if (Path.GetDirectoryName(saveFileDialog.FileName) == Path.GetDirectoryName(System.AppDomain.CurrentDomain.BaseDirectory))
                Properties.Settings.Default.LogFileName = saveFileDialog.SafeFileName;
            else
                Properties.Settings.Default.LogFileName = saveFileDialog.FileName;
        }
    }
}
