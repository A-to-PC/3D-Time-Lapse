using System.Windows;
using Microsoft.Win32;

namespace TimeLapse3D;

public partial class MainWindow : Window
{
    private readonly Settings _settings;
    private CaptureService? _capture;

    public MainWindow()
    {
        InitializeComponent();
        ThemeManager.Track(this);
        _settings = Settings.Load();
        LoadSettingsIntoUi();
    }

    private void LoadSettingsIntoUi()
    {
        RtspUrlBox.Text = _settings.RtspUrl;
        MoonrakerHostBox.Text = _settings.MoonrakerHost;
        MoonrakerPortBox.Text = _settings.MoonrakerPort.ToString();
        IntervalBox.Text = _settings.IntervalSeconds.ToString();
        OutputFolderBox.Text = _settings.OutputFolder;
        FramerateBox.Text = _settings.AssembleFramerate.ToString();
        FfmpegPathBox.Text = _settings.FfmpegPath;
    }

    private void SaveUiIntoSettings()
    {
        _settings.RtspUrl = RtspUrlBox.Text.Trim();
        _settings.MoonrakerHost = MoonrakerHostBox.Text.Trim();
        _settings.MoonrakerPort = int.TryParse(MoonrakerPortBox.Text, out var port) ? port : 7125;
        _settings.IntervalSeconds = int.TryParse(IntervalBox.Text, out var interval) ? Math.Max(1, interval) : 10;
        _settings.OutputFolder = OutputFolderBox.Text.Trim();
        _settings.AssembleFramerate = int.TryParse(FramerateBox.Text, out var fps) ? Math.Max(1, fps) : 24;
        _settings.FfmpegPath = FfmpegPathBox.Text.Trim();
        _settings.Save();
    }

    private void BrowseButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog
        {
            InitialDirectory = OutputFolderBox.Text,
        };
        if (dialog.ShowDialog() == true)
        {
            OutputFolderBox.Text = dialog.FolderName;
        }
    }

    private void StartStopButton_Click(object sender, RoutedEventArgs e)
    {
        if (_capture is { IsRunning: true })
        {
            _capture.Stop();
            StartStopButton.Content = "Start Watching";
            StatusText.Text = "Stopping...";
            return;
        }

        SaveUiIntoSettings();
        _capture = new CaptureService(_settings);
        _capture.Log += OnLog;
        _capture.Start();
        StartStopButton.Content = "Stop Watching";
        StatusText.Text = "Watching for a print...";
    }

    private void OnLog(string message)
    {
        Dispatcher.Invoke(() =>
        {
            var line = $"[{DateTime.Now:HH:mm:ss}] {message}";
            LogList.Items.Add(line);
            LogList.ScrollIntoView(line);
            StatusText.Text = message;
        });
    }
}
