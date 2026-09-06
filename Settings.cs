using System.IO;
using System.Text.Json;

namespace TimeLapse3D;

public class Settings
{
    public string RtspUrl { get; set; } = "rtsp://admin:CODE@192.168.1.100:554/ch1/main";
    public string MoonrakerHost { get; set; } = "172.16.77.13";
    public int MoonrakerPort { get; set; } = 7125;
    public int IntervalSeconds { get; set; } = 10;
    public string OutputFolder { get; set; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyVideos), "3D Time Lapse");
    public int AssembleFramerate { get; set; } = 24;
    public string FfmpegPath { get; set; } = Path.Combine(AppContext.BaseDirectory, "ffmpeg.exe");

    private static string SettingsPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "3D Time Lapse", "settings.json");

    public static Settings Load()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                var json = File.ReadAllText(SettingsPath);
                var loaded = JsonSerializer.Deserialize<Settings>(json);
                if (loaded != null) return loaded;
            }
        }
        catch
        {
            // Fall through to defaults if the file is missing or corrupt.
        }
        return new Settings();
    }

    public void Save()
    {
        var dir = Path.GetDirectoryName(SettingsPath)!;
        Directory.CreateDirectory(dir);
        var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(SettingsPath, json);
    }
}
