using System.Diagnostics;
using System.IO;

namespace TimeLapse3D;

public class CaptureService(Settings settings)
{
    private CancellationTokenSource? _cts;
    private Task? _loopTask;

    private string? _sessionFolder;
    private int _frameIndex;
    private PrintState _lastState = PrintState.Unknown;

    public event Action<string>? Log;

    public bool IsRunning => _loopTask is { IsCompleted: false };

    public void Start()
    {
        if (IsRunning) return;
        _cts = new CancellationTokenSource();
        _loopTask = RunLoopAsync(_cts.Token);
    }

    public void Stop()
    {
        _cts?.Cancel();
    }

    private async Task RunLoopAsync(CancellationToken ct)
    {
        var moonraker = new MoonrakerClient(settings.MoonrakerHost, settings.MoonrakerPort);
        Log?.Invoke($"Watching {settings.MoonrakerHost}:{settings.MoonrakerPort} every {settings.IntervalSeconds}s...");

        while (!ct.IsCancellationRequested)
        {
            PrintState state;
            try
            {
                state = await moonraker.GetPrintStateAsync(ct);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Log?.Invoke($"Moonraker poll failed: {ex.Message}");
                state = _lastState;
            }

            var wasActive = _lastState is PrintState.Printing or PrintState.Paused;
            var isActive = state is PrintState.Printing or PrintState.Paused;

            if (!wasActive && isActive)
            {
                BeginSession();
            }

            if (state == PrintState.Printing)
            {
                await CaptureFrameAsync(ct);
            }

            if (wasActive && !isActive)
            {
                await EndSessionAsync(state, ct);
            }

            _lastState = state;

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(settings.IntervalSeconds), ct);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        // If cancelled mid-print, still assemble whatever frames were captured.
        if (_sessionFolder != null)
        {
            await EndSessionAsync(PrintState.Cancelled, CancellationToken.None);
        }

        Log?.Invoke("Stopped.");
    }

    private void BeginSession()
    {
        _frameIndex = 0;
        var name = $"print_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}";
        _sessionFolder = Path.Combine(settings.OutputFolder, name, "frames");
        Directory.CreateDirectory(_sessionFolder);
        Log?.Invoke($"Print started -- capturing to {_sessionFolder}");
    }

    private async Task CaptureFrameAsync(CancellationToken ct)
    {
        if (_sessionFolder == null) return;

        _frameIndex++;
        var framePath = Path.Combine(_sessionFolder, $"frame_{_frameIndex:D5}.jpg");
        var args = $"-y -rtsp_transport tcp -i \"{settings.RtspUrl}\" -frames:v 1 -q:v 2 \"{framePath}\"";

        var ok = await RunFfmpegAsync(args, ct);
        Log?.Invoke(ok
            ? $"Captured frame {_frameIndex}"
            : $"Frame {_frameIndex} capture failed (camera unreachable?)");
    }

    private async Task EndSessionAsync(PrintState endState, CancellationToken ct)
    {
        if (_sessionFolder == null) return;

        Log?.Invoke($"Print ended ({endState}) -- assembling {_frameIndex} frames...");

        var framesGlob = Path.Combine(_sessionFolder, "frame_%05d.jpg");
        var outputFolder = Path.GetDirectoryName(_sessionFolder)!;
        var outputPath = Path.Combine(outputFolder, "timelapse.mp4");
        var args = $"-y -framerate {settings.AssembleFramerate} -i \"{framesGlob}\" -c:v libx264 -pix_fmt yuv420p \"{outputPath}\"";

        if (_frameIndex == 0)
        {
            Log?.Invoke("No frames captured -- skipping assembly.");
        }
        else
        {
            var ok = await RunFfmpegAsync(args, ct);
            Log?.Invoke(ok ? $"Timelapse saved: {outputPath}" : "Assembly failed -- check ffmpeg path/output.");
        }

        _sessionFolder = null;
        _frameIndex = 0;
    }

    private async Task<bool> RunFfmpegAsync(string arguments, CancellationToken ct)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = settings.FfmpegPath,
                Arguments = arguments,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true,
            };
            using var process = Process.Start(psi);
            if (process == null) return false;
            await process.WaitForExitAsync(ct);
            return process.ExitCode == 0;
        }
        catch (Exception ex)
        {
            Log?.Invoke($"ffmpeg error: {ex.Message}");
            return false;
        }
    }
}
