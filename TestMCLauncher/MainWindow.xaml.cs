using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Input;
using CmlLib.Core;
using CmlLib.Core.Auth;

namespace TestMCLauncher
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public ObservableCollection<string> VersionNames { get; } = new();
        public string SelectedVersion { get; set; } = string.Empty;
        public string UserName { get; set; } = "Test1234";
        private Process? _process;
        private readonly LogManager _logManager = new();
        private readonly CMLauncher _launcher;
        private readonly DataReceivedEventHandler _logEventHandler;

        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = this;
            var path = new MinecraftPath();
            _launcher = new CMLauncher(path);

            _launcher.FileChanged += (e) =>
            {
                StatusBox.AppendText($"[{e.FileKind}] {e.FileName} - {e.ProgressedFileCount}/{e.TotalFileCount}\n");
                StatusBox.ScrollToEnd();
            };
            _launcher.ProgressChanged += (s, e) => ProgressBar.Value = e.ProgressPercentage;
            _launcher.LogOutput += (s, e) =>
            {
                LogBox.AppendText($"{e}\n");
                LogBox.ScrollToEnd();
            };
            
            _launcher.GetAllVersionsAsync().ContinueWith(t =>
            {
                this.Dispatcher.Invoke(() =>
                {
                    foreach (var version in t.Result)
                    {
                        VersionNames.Add(version.Name);
                    }
                });
            });
            
            _logManager.LogAdded += (_, logRecord) =>
            {
                this.Dispatcher.Invoke(() =>
                {
                    LogBox.AppendText($"[{logRecord.Level} - {logRecord.Thread}] {logRecord.Message}\n");
                    if (!string.IsNullOrEmpty(logRecord.StackTrace))
                    {
                        LogBox.AppendText($"{logRecord.StackTrace}\n");
                    }
                    LogBox.ScrollToEnd();
                });
            };

            _logEventHandler = new DataReceivedEventHandler((s, dataEvent) =>
            {
                if (dataEvent.Data == null) return;
                _logManager.AddLog(dataEvent.Data);
            });
        }

        private async void LaunchButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(SelectedVersion))
            {
                MessageBox.Show("Please select a version");
                return;
            }
            if (string.IsNullOrEmpty(UserName))
            {
                MessageBox.Show("Please enter a username");
                return;
            }
            var launchOption = new MLaunchOption
            {
                MaximumRamMb = 1024,
                Session = MSession.GetOfflineSession(UserName)
            };
            LaunchButton.IsEnabled = false;
            _process = await _launcher.CreateProcessAsync(SelectedVersion, launchOption);
            _process.StartInfo.RedirectStandardOutput = true;
            _process.StartInfo.RedirectStandardError = true;
            _process.StartInfo.UseShellExecute = false;
            _process.StartInfo.StandardErrorEncoding = Encoding.UTF8;
            _process.StartInfo.StandardOutputEncoding = Encoding.UTF8;
            _process.OutputDataReceived += _logEventHandler;
            _process.ErrorDataReceived += _logEventHandler;
            _process.EnableRaisingEvents = true;
            _process.Exited += (_, _) =>
            {
                _process.OutputDataReceived -= _logEventHandler;
                _process.ErrorDataReceived -= _logEventHandler;
                _process = null;
                this.Dispatcher.Invoke(() =>
                {
                    LaunchButton.IsEnabled = true;
                });
            };
            _process.Start();
            _process.BeginOutputReadLine();
            _process.BeginErrorReadLine();
        }

        private void CloseButton_OnClick(object sender, RoutedEventArgs e)
        {
            _process?.Kill();
            this.Close();
        }

        private void MainWindow_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
            }
        }
    }
}
