using System;
using System.Linq;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Input;
using System.Windows.Media;
using System.IO;
using System.IO.Ports;
using Microsoft.Win32;
using System.Threading.Tasks;
using System.Threading;
using System.Windows.Threading;

namespace MCUTerm
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        private SerialPort serialPort = new SerialPort();
        private List<string> availablePorts = new List<string>();

        private string pendingText = "";

        private string status;
        private string Status
        {
            get => status;
            set
            {
                string newTitle = "MCUTerm";
                if (value.Length > 0)
                    newTitle += "  -  " + value;

                status = value;
                Title = newTitle;
            }
        }

        public MainWindow()
        {
            InitializeComponent();
            ComponentDispatcher.ThreadIdle += ComponentDispatcher_ThreadIdle;
        }

        // Actions and Events
        private void ComponentDispatcher_ThreadIdle(object sender, EventArgs e)
        {
            UpdateAvailablePorts();

            if (serialPort.IsOpen)
            {
                ConnectButton.IsEnabled = false;
                RestartButton.IsEnabled = true;
                DisconnectButton.IsEnabled = true;
                SendButton.IsEnabled = true;
                SendFileButton.IsEnabled = true;
            }
            else
            {
                ConnectButton.IsEnabled = true;
                RestartButton.IsEnabled = false;
                DisconnectButton.IsEnabled = false;
                SendButton.IsEnabled = false;
                SendFileButton.IsEnabled = false;
            }

            if (pendingText.Length > 0)
            {
                AppendConsole(pendingText);
                pendingText = "";
            }
        }

        private void OptionsButton_Click(object sender, RoutedEventArgs e) => ShowSettings();
        private void ShowSettings()
        {
            SettingsWindow settingsWindow = new SettingsWindow();
            settingsWindow.Owner = this;

            Properties.Settings.Default.Save();
            if (settingsWindow.ShowDialog() == false)
                Properties.Settings.Default.Reload();
        }

        private void ConnectButton_Click(object sender, RoutedEventArgs e) => OpenSelectedPort();
        private void RestartButton_Click(object sender, RoutedEventArgs e)
        {
            serialPort.RtsEnable = true;
            Thread.Sleep(1);
            serialPort.RtsEnable = false;
        }

        private void StopButton_Click(object sender, RoutedEventArgs e) => Stop();
        private void ClearButton_Click(object sender, RoutedEventArgs e) => ConsoleText.ClearText();
        private void SendButton_Click(object sender, RoutedEventArgs e) => Send();

        private void ConsoleText_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Filter.Text = "";
                ConsoleText.SelectionLength = 0;
            }
        }

        private void SendText_KeyUp(object sender, KeyEventArgs e)
        {
            switch(e.Key)
            {
                case Key.Enter:
                    Send();
                    break;
                case Key.Escape:
                    SendText.Text = "";
                    break;
            }
        }

        private void Filter_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                Filter.Text = "";
        }

        private void SendFileButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.CheckFileExists = true;
            openFileDialog.Multiselect = false;
            openFileDialog.Title = "Select Data File";
            openFileDialog.InitialDirectory = System.AppDomain.CurrentDomain.BaseDirectory;

            if (openFileDialog.ShowDialog() == true)
            {
                string text = File.ReadAllText(openFileDialog.FileName);
                serialPort.Write(text);

                AddLocalEcho(text);
            }
        }

        public void Stop()
        {
            serialPort.Close();
            Status = "Disconnected";
        }

        public void OpenPort(string portName, string baudRate)
        {
            UpdateAvailablePorts();

            PortList.SelectedItem = portName;
            Properties.Settings.Default.BaudRate = baudRate;

            OpenSelectedPort();
        }

        private void OpenSelectedPort()
        {
            serialPort.Close();
            status = "";

            if (Properties.Settings.Default.AfterConnectIndex == 1)
                ConsoleText.ClearText();
            
            try
            {
                const int cDataBitsMin = 5;

                serialPort.PortName = Properties.Settings.Default.PortName;
                serialPort.BaudRate = int.Parse(Properties.Settings.Default.BaudRate);
                serialPort.Parity = (Parity)Properties.Settings.Default.ParityIndex;
                serialPort.DataBits = Properties.Settings.Default.DataBitsIndex + cDataBitsMin;
                serialPort.StopBits = (StopBits)Properties.Settings.Default.StopBitsIndex;
                serialPort.Handshake = (Handshake)Properties.Settings.Default.HandshakeIndex;

                // Set the read/write timeouts
                serialPort.ReadTimeout = 100;
                serialPort.WriteTimeout = 100;

                serialPort.DataReceived += SerialPort_DataReceived;
                serialPort.ErrorReceived += SerialPort_ErrorReceived;

                serialPort.Open();
            }
            catch (IOException)
            {
                Status = String.Format("{0} does not exist", Properties.Settings.Default.PortName);
            }
            catch (UnauthorizedAccessException)
            {
                Status = String.Format("{0} already in use", Properties.Settings.Default.PortName);
            }
            catch (Exception ex)
            {
                Status = String.Format("{0}", ex.ToString());
            }

            if (serialPort.IsOpen)
                Status = "Connected";
            else if (Status == "")
                Status = "Unknown error";

            if (IsActive == false)
                ActivateAsync();
        }

        /// <summary>
        /// Activate main window.
        /// </summary>
        /// <remarks>
        /// The trick with tompost and delay is used to activate window in all cases.
        /// Otherwise it may just blick in the taskbar.
        /// </remarks>
        private async void ActivateAsync()
        {
            Topmost = true;
            Activate();
            await Task.Delay(1000);
            Topmost = false;
        }

        private void SerialPort_ErrorReceived(object sender, SerialErrorReceivedEventArgs e)
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(() => Status = "Connection Error"));
        }

        private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs args)
        {
            string received;
            try
            {
                received = serialPort.ReadExisting();
                if (received.Length == 0)
                    return;
            }
            catch (InvalidOperationException)
            {
                // port can be closed in case of Stop for example
                return;
            }

            // BeginInvoke is used because Invoke may result in deadlock (serialPort.Close() in main thread during invoke)
            Application.Current.Dispatcher.BeginInvoke(new Action(() => pendingText += received) );

            try
            {
                if (Properties.Settings.Default.LogFileName.Length > 0)
                    File.AppendAllText(Properties.Settings.Default.LogFileName, received);
            }
            catch(Exception e)
            {
                if (logFileErrordDispatcher == null || logFileErrordDispatcher.Status == DispatcherOperationStatus.Completed)
                    logFileErrordDispatcher = Application.Current.Dispatcher.BeginInvoke(new Action(() => OnLogFileError(e)));
            }
        }

        private DispatcherOperation logFileErrordDispatcher = null;
        private void OnLogFileError(Exception e)
        {
            MessageBox.MessageBoxButton result =
                MessageBox.Show("<Bold>Log File Error</Bold><LineBreak/><LineBreak/>" + e.Message, "New Log File", "Stop Logging", MessageBox.MessageBoxButton.Button3, MessageBox.MessageBoxButton.Button3, "Continue");
            
            if (result == MessageBox.MessageBoxButton.Button1)
                ShowSettings();

            if (result == MessageBox.MessageBoxButton.Button2)
                Properties.Settings.Default.LogFileName = "";
        }

        private void AppendConsole(string text)
        {
            AppendConsoleText(text);

            if (ConsoleText.SelectedText.Length <= 0 && Properties.Settings.Default.AutoScroll)
                ConsoleText.ScrollToBottom();
        }

        // The names of the first 32 characters
        private static readonly string[] charNames = {"NUL", "SOH", "STX", "ETX", "EOT",
                "ENQ", "ACK", "BEL", "BS", "", "", "VT", "FF", "", "SO", "SI",
                "DLE", "DC1", "DC2", "DC3", "DC4", "NAK", "SYN", "ETB", "CAN", "EM", "SUB",
                "ESC", "FS", "GS", "RS", "US", "Space"};

        private void AppendConsoleText(string text)
        {
            bool hexOutput = Properties.Settings.Default.HexOutput;

            string newText = "";
            for (int pos = 0; pos < text.Length; pos++)
            {
                Char c = text[pos];

                if (hexOutput)
                    newText += String.Format("{0:X2} ", (int)c);
                else if (c < 32 && charNames[c] != "")
                {
                    ConsoleText.AddText(newText);
                    ConsoleText.AddText('<' + charNames[c] + '>', Brushes.OrangeRed, null);

                    newText = "";
                }
                else
                    newText += c;
            }

            ConsoleText.AddText(newText);
        }

        private enum SendPostifx {AppendNothing = 0, AppendCR, AppendLF, AppendCRLF};

        private void AddLocalEcho(string text)
        {
            if (Properties.Settings.Default.LocalEcho)
                ConsoleText.AddText(text + "\n", null, Brushes.DarkGreen);
        }

        private void Send()
        {
            if (SendText.Text.Length == 0 || serialPort.IsOpen == false)
                return;

            string linePostfix;
            switch ((SendPostifx)Properties.Settings.Default.SendPostfixIndex)
            {
                case SendPostifx.AppendCR:
                    linePostfix = "\r";
                    break;
                case SendPostifx.AppendLF:
                    linePostfix = "\n";
                    break;
                case SendPostifx.AppendCRLF:
                    linePostfix = "\r\n";
                    break;
                default:
                    linePostfix = "";
                    break;
            }

            serialPort.Write(System.Text.RegularExpressions.Regex.Unescape(SendText.Text) + linePostfix);

            AddLocalEcho(SendText.Text);
            SendText.Text = "";
        }

        private void UpdateAvailablePorts()
        {
            if (availablePorts.SequenceEqual(SerialPort.GetPortNames()) == true)
                return;

            PortList.ItemsSource = availablePorts = new List<string>(SerialPort.GetPortNames());
            PortList.SelectedValue = Properties.Settings.Default.PortName;
        }

        private void PortList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Convert.ToString(PortList.SelectedItem).Length > 0)
                Properties.Settings.Default.PortName = Convert.ToString(PortList.SelectedItem);
        }

        // Deal with smart resize
        protected override Size MeasureOverride(Size availableSize)
        {
            System.Diagnostics.Debug.Assert(VisualChildrenCount == 1, "Check xaml! The Window with smart sizing should have only one direct child.");

            FrameworkElement element = (FrameworkElement)GetVisualChild(0);
            element.Measure(availableSize);

            Size desiredSize = element.DesiredSize;
            if (desiredSize.Width != MinWidth)
            {
                if (SizeToContent != SizeToContent.Width && ActualWidth == MinWidth)
                    SizeToContent = SizeToContent.Width;

                MinWidth = desiredSize.Width;
            }

            return desiredSize;
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (SizeToContent != SizeToContent.Width && e.NewSize.Width < MinWidth + 1)
                SizeToContent = SizeToContent.Width;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (ActualWidth < Properties.Settings.Default.MainWindowWidth)
                SizeToContent = SizeToContent.Manual;

            if (Properties.Settings.Default.AtStartupIndex == 1 && serialPort.IsOpen == false)
                OpenSelectedPort();
        }
    }
}
