# 3D Time Lapse

A small standalone Windows app that watches a [Moonraker](https://github.com/Arksine/moonraker)-enabled 3D printer for print state and automatically records a timelapse from any RTSP camera — no Klipper config changes required.

Running an Anycubic Kobra 3-series printer on **stock** firmware, without Rinkhals/Moonraker? See [Kobra Time Lapse](https://github.com/A-to-PC/Kobra-Time-Lapse) instead — that's this project's stock-firmware sibling, talking to the printer's own LAN protocol directly rather than polling Moonraker.

**Status:** stable, not actively updated — the maintainer moved to stock firmware, so new feature work is happening on Kobra Time Lapse instead. That's a "not right now," not a "never": if Rinkhals goes back on, this is the one that reopens.

Part of a small family of tools built out of real Kobra 3 Max ownership — see [Kobra 3 Max: The Long Way Round](https://github.com/A-to-PC/Kobra-3-Max-Journey) for the full story of why this exists.

## Why this exists

The obvious way to get a print-triggered timelapse is [moonraker-timelapse](https://github.com/mainsail-crew/moonraker-timelapse), a plugin that runs *inside* Moonraker/Klipper. That works well on standard installs, but on heavily customized firmware (e.g. [Rinkhals](https://github.com/rinkhals-community/Rinkhals) on Anycubic Kobra printers) it has a track record of crashing Klipper outright when added, with reports going back a long way and no confirmed fix.

3D Time Lapse takes a different approach: it never touches Klipper or Moonraker's config at all. It only ever makes **read-only** polling requests to Moonraker's REST API to check `print_stats.state`, and separately talks to your camera over RTSP. If the printer's firmware is fragile, this app can't make it worse — there's nothing to install on the printer side.

## How it works

1. Polls `GET /printer/objects/query?print_stats` on your Moonraker instance every N seconds (your choice).
2. When state transitions into `printing`, it starts a new capture session and grabs one JPEG frame from your RTSP camera every interval via `ffmpeg`.
3. Capture pauses automatically while the printer is `paused` (no duplicate frames of a stalled print), and resumes when printing continues.
4. The moment state moves to `complete`, `cancelled`, `error`, or back to `standby`, it stops and automatically assembles the captured frames into `timelapse.mp4` with `ffmpeg`.

No slicer time estimates, no fixed timers — it goes purely off the printer's actual reported state.

## Requirements

- Windows 10/11
- [.NET 10 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/10.0) (or build from source with the .NET 10 SDK)
- A printer running Moonraker, reachable on your network
- An RTSP-capable camera (most consumer WiFi/NVR cameras support this once enabled in their settings)
- `ffmpeg.exe` — either on your system `PATH`, or dropped next to the app's `.exe` (the release download includes one)

## Setup

1. Download the latest release, extract it anywhere, and run `TimeLapse3D.exe`.
2. Fill in:
   - **RTSP camera URL** — e.g. `rtsp://user:password@192.168.1.50:554/ch1/main` (varies by camera brand — check your camera's docs for its exact RTSP path)
   - **Moonraker host / IP** and **port** (Moonraker's default port is `7125`)
   - **Poll/capture interval** in seconds
   - **Output folder** — each print gets its own timestamped subfolder with the raw frames and the final `timelapse.mp4`
   - **Assemble FPS** — the framerate of the final timelapse video
   - **ffmpeg path** — leave as `ffmpeg` if it's on your `PATH`, or point at a specific `ffmpeg.exe`
3. Click **Start Watching** and leave it running. Settings are remembered between launches.

## Building from source

```
dotnet build -c Release
```

The build output includes `ffmpeg.exe` automatically if one is present in the project root at build time — supply your own if you're building from a fresh clone (not included in source control due to its size).

## License

MIT — see [LICENSE](LICENSE).
