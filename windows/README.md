# Flux Pointer for Windows

Flux Pointer for Windows adds the animated Flux halo, color-shifting particle
trail, and click rupture to the normal pointer across the Windows desktop and
ordinary apps.

## Run it

1. Download `FluxPointer.exe` from the latest GitHub release.
2. Double-click it. No installation or administrator access is required.
3. Right-click the colorful Flux icon in the system tray to pause the effects,
   start Flux Pointer with Windows, open the web demo, or exit.

Windows SmartScreen may show a warning because this free download is not
digitally signed. Choose **More info**, then **Run anyway** if you downloaded it
from this repository.

## What it does

- The visual changes continuously with pointer speed, direction, and time.
- Movement leaves a fading chromatic particle trail.
- Left-click creates a starburst and rotating shockwave.
- Right-click creates a larger alternate rupture.
- The overlay is click-through, so it does not interfere with normal input.
- It does not use the network, collect data, or require admin rights.

The regular Windows pointer remains visible for dependable clicking. Windows
secure screens and some exclusive fullscreen games do not allow ordinary apps
to draw overlays.

## Build from source

On Windows, run:

```powershell
powershell -ExecutionPolicy Bypass -File .\build.ps1
```

The app is compiled with the C# compiler included with Windows and is written to
`dist\FluxPointer.exe`.

## Compatibility

Windows 10 or Windows 11 with .NET Framework 4.8.
