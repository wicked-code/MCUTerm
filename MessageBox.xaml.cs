using System.Linq;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Markup;

namespace MCUTerm
{
    /// <summary>
    /// Interaction logic for MessageBox.xaml
    /// </summary>
    public partial class MessageBox : Window
    {
        public enum MessageBoxButton
        {
            None,
            Button1,
            Button2,
            Button3,
            Button4
        }

        private MessageBoxButton messageBoxResult = MessageBoxButton.None;
        private MessageBoxButton MessageBoxResult
        {
            get => messageBoxResult;
            set
            {
                DialogResult = true;
                messageBoxResult = value;
            }
        }

        private MessageBox(string text, string button1Text, string button2Text,
                           MessageBoxButton defaultButton, MessageBoxButton cancelButton,
                           string button3Text, string button4Text)
        {
            InitializeComponent();

            if (Application.Current.MainWindow == this)
                WindowStartupLocation = WindowStartupLocation.CenterScreen;
            else
                Owner = Application.Current.MainWindow;

            switch (defaultButton)
            {
                case MessageBoxButton.Button1:
                    Button1.IsDefault = true;
                    break;

                case MessageBoxButton.Button2:
                    Button2.IsDefault = true;
                    break;

                case MessageBoxButton.Button3:
                    Button3.IsDefault = true;
                    break;

                case MessageBoxButton.Button4:
                    Button4.IsDefault = true;
                    break;
            }

            switch (cancelButton)
            {
                case MessageBoxButton.Button1:
                    Button1.IsCancel = true;
                    break;

                case MessageBoxButton.Button2:
                    Button2.IsCancel = true;
                    break;

                case MessageBoxButton.Button3:
                    Button3.IsCancel = true;
                    break;

                case MessageBoxButton.Button4:
                    Button4.IsCancel = true;
                    break;
            }

            try
            {
                var span = (Span)XamlReader.Parse("<Span xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\">" + text + "</Span>");
                Message.Inlines.AddRange(span.Inlines.ToArray());
            }
            catch
            {
                Message.Text = text;
            }

            Button1.Content = button1Text;
            Button2.Content = button2Text;
            Button3.Content = button3Text;
            Button4.Content = button4Text;

            if (button2Text == "")
                Button2.Visibility = (button1Text == "") ? Visibility.Collapsed : Visibility.Hidden;

            if (button3Text == "")
                Button3.Visibility = (button2Text == "") ? Visibility.Collapsed : Visibility.Hidden;

            if (button4Text == "")
                Button4.Visibility = (button4Text == "") ? Visibility.Collapsed : Visibility.Hidden;
        }

        public static MessageBoxButton Show(string text, string button1Text = "Ok", string button2Text = "",
                                            MessageBoxButton defaultButton = MessageBoxButton.Button1,
                                            MessageBoxButton cancelButton = MessageBoxButton.Button2,
                                            string button3Text = "", string button4Text = "")
        {
            var messageBox = new MessageBox(text, button1Text, button2Text, defaultButton, cancelButton, button3Text, button4Text);

            messageBox.ShowDialog();
            return messageBox.MessageBoxResult;
        }

        private void Button1_Click(object sender, RoutedEventArgs e) => MessageBoxResult = MessageBoxButton.Button1;
        private void Button2_Click(object sender, RoutedEventArgs e) => MessageBoxResult = MessageBoxButton.Button2;
        private void Button3_Click(object sender, RoutedEventArgs e) => MessageBoxResult = MessageBoxButton.Button3;
        private void Button4_Click(object sender, RoutedEventArgs e) => MessageBoxResult = MessageBoxButton.Button4;
    }
}
