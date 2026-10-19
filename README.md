# PodShift

**One-click AirPods switching for Windows.**

PodShift is a tiny Windows utility that finds your paired AirPods, shows their connection status, and experimentally switches the stereo A2DP profile to or from your PC without making you dig through Windows Bluetooth settings every time.

> **Status: v0.2 alpha.** One-click A2DP connect/disconnect is implemented and the Windows build passes GitHub Actions. Bluetooth behavior can still vary between PCs, adapters, Windows versions, and AirPods models, so this is an experimental build rather than a promise that Windows Bluetooth has suddenly become reasonable.

<!-- TODO: add screenshot / GIF here -->
<!-- ![PodShift screenshot](Assets/screenshot.png) -->

## The problem

AirPods are excellent at moving between Apple devices. Windows is less enthusiastic.

If your AirPods are connected to an iPhone and you want to use them on a Windows PC, the normal routine is usually:

1. Open Settings.
2. Open Bluetooth.
3. Find the right device.
4. Connect it.
5. Repeat the ritual next time.

PodShift tries to reduce that to one small window and one click.

## What v0.2 does

- Finds AirPods among already-paired Bluetooth devices.
- Prioritizes AirPods Pro, AirPods Max, and AirPods by name.
- Shows the current Windows connection status.
- Attempts one-click connection by resetting and re-enabling the A2DP stereo service.
- Attempts one-click disconnect by disabling the A2DP stereo service.
- Uses the actual local Bluetooth radio handle for the native Windows Bluetooth calls.
- Waits for Windows to report the resulting connection state instead of assuming success.
- Refreshes status when the app window becomes active again.
- Falls back to Windows Bluetooth settings if the native operation cannot be completed.

## How it works

Windows does not expose a simple public API such as `ConnectBluetoothAudioDevice(deviceId)` for an already-paired audio device.

PodShift therefore uses two layers:

- **WinRT Bluetooth APIs** to discover paired devices, read their Bluetooth address, and check `ConnectionStatus`.
- **Win32 Bluetooth APIs** from `bthprops.cpl` to locate the Bluetooth radio and toggle the A2DP service with `BluetoothSetServiceState`.

For a connect attempt, PodShift currently:

1. Reads the AirPods Bluetooth address.
2. Finds the local Bluetooth radio that knows the device.
3. Disables the A2DP service.
4. Waits one second.
5. Enables A2DP again.
6. Waits up to 10 seconds for Windows to report the AirPods as connected.

For disconnect, it disables A2DP and waits for Windows to report the device as disconnected.

This is intentionally an **A2DP-first** implementation. Hands-Free / headset mode is not toggled in v0.2 because the first goal is reliable stereo audio switching.

## Requirements

- Windows 10 or Windows 11.
- x64 PC.
- Working Bluetooth adapter/radio.
- AirPods must already be paired with Windows once through the normal Bluetooth settings.

PodShift does **not** perform initial Bluetooth pairing.

## Current limitations

- One-click switching depends on the Windows Bluetooth stack and may behave differently with different Bluetooth adapters or drivers.
- If another device is actively holding the AirPods connection, Windows may fail to take them within the timeout.
- v0.2 automatically chooses the highest-scoring AirPods device it finds. There is no device picker yet.
- Only the stereo A2DP path is targeted right now. Microphone / Hands-Free mode is not managed.
- There is no tray icon yet.
- The project does not yet have a polished installer or signed executable.

If one-click fails, PodShift opens the normal Windows Bluetooth settings as a fallback.

## Download

For the current alpha builds:

1. Open the **Actions** tab.
2. Open the latest successful **Build PodShift for Windows** run.
3. Download the `PodShift-win-x64` artifact.
4. Extract the ZIP and run `PodShift.exe`.

The GitHub Actions build is self-contained, so a separate .NET installation is not required.

A normal GitHub Release with a ready-to-download executable is planned once the one-click behavior has been tested on more machines.

## Build from source

```bash
git clone https://github.com/agbabenkov-del/PodShift.git
cd PodShift
dotnet restore PodShift.csproj
dotnet build PodShift.csproj -c Release
```

To create the same self-contained Windows build used by GitHub Actions:

```bash
dotnet publish PodShift.csproj \
  -c Release \
  -r win-x64 \
  --self-contained true \
  -p:PublishSingleFile=true \
  -p:IncludeNativeLibrariesForSelfExtract=true \
  -o publish
```

## Tech

- C#
- .NET 8
- WPF / XAML
- Windows.Devices.Bluetooth / WinRT
- Win32 Bluetooth APIs via P/Invoke
- GitHub Actions for Windows builds

The Bluetooth switching logic lives in [`BluetoothService.cs`](BluetoothService.cs).

## Roadmap

- [x] Find paired AirPods
- [x] Show connection status
- [x] Experimental one-click A2DP connect
- [x] Experimental one-click A2DP disconnect
- [x] Automatic status refresh when returning to the app
- [ ] Test reliability across more AirPods models, Windows versions, and Bluetooth adapters
- [ ] Device picker for users with multiple AirPods
- [ ] Tray icon and quick switching
- [ ] Screenshot / demo GIF
- [ ] Tagged GitHub releases with ready-to-run builds
- [ ] Installer / code signing if the project grows beyond the tiny-utility stage

## Testing and feedback

Real-world Bluetooth reports are especially useful. If you test PodShift, an issue containing the following is enough:

- AirPods model
- Windows version
- Bluetooth adapter or laptop model, if known
- Whether Connect worked
- Whether Disconnect worked
- What PodShift displayed if it failed

Issues and pull requests are welcome.

## License

A license file has not been added yet. Until one is added, the repository should not be treated as carrying an open-source license by default.
