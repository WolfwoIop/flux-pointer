# Flux Pointer for Windows

Flux Pointer for Windows adds the animated Flux halo, color-shifting particle
trail, and click rupture to the normal pointer across the Windows desktop and
ordinary apps.

## Run it

1. Download `FluxPointer.exe` from the latest GitHub release.
2. Double-click it. No installation or administrator access is required.
3. Click the colorful Flux icon in the system tray to choose a skin, pause the
   effects, start Flux Pointer with Windows, open the web demo, or exit.

Windows SmartScreen may show a warning because this free download is not
digitally signed. Choose **More info**, then **Run anyway** if you downloaded it
from this repository.

## What it does

- The visual changes continuously with pointer speed, direction, and time.
- Movement leaves a fading chromatic particle trail.
- Left-click creates a starburst and rotating shockwave.
- Right-click creates a larger alternate rupture.
- The overlay is click-through, so it does not interfere with normal input.
- The normal Windows cursor is hidden by default so the Flux field becomes the
  pointer. You can restore it from the tray menu at any time.
- After three seconds without movement or clicks, the Flux pointer fades out
  completely. Moving or clicking wakes it immediately.
- It does not use the network, collect data, or require admin rights.

## Skins

Open **Skins** from the tray menu and choose:

- **Spectrum Drift** — full-spectrum color cycling with an elliptical orbit.
- **Acid Ghost** — toxic green tones with broken circular arcs.
- **Solar Flare** — amber and red light with radiating sun spikes.
- **Ice Signal** — cyan-blue particles with rotating diamond geometry.
- **Mono Pulse** — a minimal white signal with concentric dotted rings.

Your choice is remembered the next time Flux Pointer starts.

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
