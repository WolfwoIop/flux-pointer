# Flux Pointer for Windows

Flux Pointer for Windows adds the animated Flux halo, color-shifting particle
trail, and click rupture to the normal pointer across the Windows desktop and
ordinary apps.

## Run it

1. Download `FluxPointer.exe` from the latest GitHub release.
2. Double-click it. No installation or administrator access is required.
3. Click the colorful Flux icon in the system tray to choose a skin, pause the
   effects, adjust visual intensity, start Flux Pointer with Windows, open the
   web demo, or exit. Double-click the tray icon to pause or resume instantly.

Windows SmartScreen may show a warning because this free download is not
digitally signed. Choose **More info**, then **Run anyway** if you downloaded it
from this repository.

## What it does

- The visual changes continuously with pointer speed, direction, and time.
- Movement leaves a fading chromatic particle trail.
- Left-click creates a starburst and rotating shockwave.
- Right-click creates a larger alternate rupture.
- Middle-click also creates the larger alternate rupture.
- The overlay is click-through, so it does not interfere with normal input.
- The overlay reasserts its always-on-top position when you switch browser tabs
  or bring another ordinary window forward.
- The normal Windows cursor is hidden by default so the Flux field becomes the
  pointer. You can restore it from the tray menu at any time.
- After three seconds without movement or clicks, the Flux pointer fades out
  completely. Moving or clicking wakes it immediately.
- The renderer reuses its drawing surface and automatically lowers its refresh
  rate while hidden or paused, reducing idle resource use.
- Per-monitor DPI support keeps the pointer aligned when monitors use different
  Windows scaling settings.
- Locking Windows restores the system cursor safely; unlocking wakes Flux
  Pointer again.
- It does not use the network, collect data, or require admin rights.

## Skins

Open **Choose skin** from the tray menu and choose:

- **Spectrum Drift** — full-spectrum color cycling with an elliptical orbit.
- **Acid Ghost** — toxic green bubbles, spectral crescents, and broken arcs.
- **Solar Flare** — ember streaks, triangular sparks, and a radiating sun core.
- **Ice Signal** — diamond shards, snowflake bursts, and crystalline geometry.
- **Mono Pulse** — square pixels, cross-shaped bursts, and a high-contrast crosshair.

Your choice is remembered the next time Flux Pointer starts.

## Visual intensity

Open **Visual intensity** from the tray menu and choose:

- **Calm** — fewer particles and the lowest resource use.
- **Balanced** — the default mix of detail and performance.
- **Vivid** — denser trails and larger bursts.

Flux Pointer remembers both the intensity and whether you prefer the normal
Windows cursor hidden.

Pausing or exiting Flux Pointer automatically restores the normal Windows
cursor. Windows secure screens and some exclusive fullscreen games do not allow
ordinary apps to draw overlays.

## Build from source

On Windows, run:

```powershell
powershell -ExecutionPolicy Bypass -File .\build.ps1
```

The app is compiled with the C# compiler included with Windows and is written to
`dist\FluxPointer.exe`.

## Compatibility

Windows 10 or Windows 11 with .NET Framework 4.8.
