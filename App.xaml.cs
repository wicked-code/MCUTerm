using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;


namespace MCUTerm
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private Task pipeServerTask;
        private CancellationTokenSource ctsPipeServer;

        private const int MaxInstancesNumber = 5;
        private const string PipeBaseName = "MCUTermPIPE";
        private const string CommandConnect = "connect";
        private const string CommandDisconnect = "disconnect";
        private const string OptionRun = "run";

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            if (e.Args.Length == 0)
            {
                ctsPipeServer = new CancellationTokenSource();
                pipeServerTask = Task.Run(() => PipeServerTask());
                return;
            }

            switch(e.Args[0].ToLower())
            {
                case CommandConnect:
                    if (e.Args.Length == 3)
                    {
                        SendCommands(new string[] { CommandConnect, e.Args[1], e.Args[2] }, false);
                        Shutdown();
                        return;
                    }
                    else if (e.Args.Length == 4 && e.Args[1].ToLower() == OptionRun)
                    {
                        SendCommands(new string[] { CommandConnect, e.Args[2], e.Args[3] }, true);
                        Shutdown();
                        return;
                    }

                    break;

                case CommandDisconnect:
                    if (e.Args.Length == 1)
                    {
                        SendCommands(new string[] { CommandDisconnect });
                        Shutdown();
                        return;
                    }

                    break;
            }

            MessageBox.Show("<Bold>Invalid Command Line</Bold><LineBreak/><LineBreak/>" +
                            "Usage:  MCUTerm {[Disconnect] [Connect [Run] &lt;PortName&gt; &lt;BaudRate&gt;]}<LineBreak/>" +
                            "Connect - Connect to PortName using BaudRate.<LineBreak/>" +
                            "Run - Start new instance of MCUTerm if it doesn't exist.<LineBreak/>" +
                            "Disconnect - Disconnect from connected port.<LineBreak/><LineBreak/>" +
                            "Any command line option prevents MCUTerm from start.<LineBreak/>" +
                            "It will command another instance of MCUTerm and then exit." +
                            "<LineBreak/><LineBreak/>" +
                            "Example: MCUTerm Connect COM20 115200<LineBreak/>");
            Shutdown();
        }

        private void PipeServerTask()
        {
            NamedPipeServerStream pipeServer = null;
            for (int i = 0; i < MaxInstancesNumber; i++)
            {
                try
                {
                    pipeServer = new NamedPipeServerStream(PipeBaseName + i, PipeDirection.In, 1, PipeTransmissionMode.Message);
                    break;
                }
                catch (IOException) { }
            }

            try
            {
                while (pipeServer != null)
                {
                    Task connectionTask = pipeServer.WaitForConnectionAsync(ctsPipeServer.Token);
                    connectionTask.Wait(ctsPipeServer.Token);

                    if (pipeServer.IsConnected)
                        ProcessCommandFromPipe(new StreamReader(pipeServer));

                    pipeServer.Disconnect();
                }
            }
            catch (OperationCanceledException) { }
        }

        private void ProcessCommandFromPipe(StreamReader reader)
        {
            string command = reader.ReadLine();
            switch (command)
            {
                case CommandConnect:
                    string portName = reader.ReadLine();
                    string baudRate = reader.ReadLine();
                    Application.Current.Dispatcher.Invoke(() => ((MainWindow)MainWindow).OpenPort(portName, baudRate));
                    break;
                case CommandDisconnect:
                    Application.Current.Dispatcher.Invoke(() => ((MainWindow)MainWindow).Stop());
                    break;
            }
        }

        private void SendCommands(string[] commands, bool launch = false)
        {
            for (int i = 0; i < MaxInstancesNumber; i++)
            {
                try
                {
                    var pipeClient = new NamedPipeClientStream(".", PipeBaseName + i, PipeDirection.Out);
                    pipeClient.Connect(launch ? 10 : 100);

                    var writer = new StreamWriter(pipeClient);
                    foreach (var command in commands)
                        writer.WriteLine(command);

                    writer.Close();
                    pipeClient.Close();
                    return;
                }
                catch (TimeoutException) { }
                catch (IOException) { }
            }

            if (launch == true)
            {
                Process p = new Process();
                p.StartInfo.FileName = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
                p.Start();
                p.WaitForInputIdle();

                SendCommands(commands);
            }
        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            if (ctsPipeServer != null)
                ctsPipeServer.Cancel();

            if (pipeServerTask != null)
                pipeServerTask.Wait();

            MCUTerm.Properties.Settings.Default.Save();
        }
    }

    namespace Properties
    {
        sealed partial class Settings
        {
            [global::System.Configuration.UserScopedSettingAttribute()]
            [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
            [global::System.Configuration.DefaultSettingValueAttribute(@"<?xml version=""1.0"" encoding=""utf-16""?>
<ArrayOfString xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"">
  <string>9600</string>
  <string>14400</string>
  <string>57600</string>
  <string>74880</string>
  <string>115200</string>
</ArrayOfString>")]
            public global::System.Collections.ObjectModel.ObservableCollection<string> BaudList
            {
                get
                {
                    return ((global::System.Collections.ObjectModel.ObservableCollection<string>)(this["BaudList"]));
                }
                set
                {
                    this["BaudList"] = value;
                }
            }
        }
    }
}
