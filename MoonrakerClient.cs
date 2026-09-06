using System.Net.Http;
using System.Text.Json;

namespace TimeLapse3D;

public enum PrintState
{
    Unknown,
    Standby,
    Printing,
    Paused,
    Complete,
    Cancelled,
    Error,
}

public class MoonrakerClient(string host, int port)
{
    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(5) };
    private readonly string _baseUrl = $"http://{host}:{port}";

    public async Task<PrintState> GetPrintStateAsync(CancellationToken ct)
    {
        var url = $"{_baseUrl}/printer/objects/query?print_stats";
        using var response = await _http.GetAsync(url, ct);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

        var state = doc.RootElement
            .GetProperty("result")
            .GetProperty("status")
            .GetProperty("print_stats")
            .GetProperty("state")
            .GetString();

        return state switch
        {
            "standby" => PrintState.Standby,
            "printing" => PrintState.Printing,
            "paused" => PrintState.Paused,
            "complete" => PrintState.Complete,
            "cancelled" => PrintState.Cancelled,
            "error" => PrintState.Error,
            _ => PrintState.Unknown,
        };
    }
}
