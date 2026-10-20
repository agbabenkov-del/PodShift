# Contributing to PodShift

Thanks for helping make Windows Bluetooth slightly less ceremonial.

## Before opening a pull request

1. Open an issue first for larger behavior changes or UI redesigns.
2. Keep changes focused. Small, reviewable pull requests are preferred.
3. Do not remove the Bluetooth Settings fallback unless the replacement has been tested on real hardware.
4. Preserve the current A2DP-first behavior unless a change explicitly targets headset/microphone support.

## Development setup

Requirements:

- Windows 10 or Windows 11
- .NET 8 SDK
- A Bluetooth adapter for real device testing

Build:

```powershell
dotnet restore PodShift.csproj
dotnet build PodShift.csproj -c Release
```

Publish the self-contained x64 build:

```powershell
dotnet publish PodShift.csproj `
  -c Release `
  -r win-x64 `
  --self-contained true `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  -o publish
```

## Testing Bluetooth changes

For changes touching `BluetoothService.cs`, please test as much of this sequence as your hardware allows:

1. AirPods are paired with Windows but currently connected to another device.
2. PodShift finds the expected AirPods.
3. **Connect to PC** transfers audio to Windows.
4. Windows reports the device as connected.
5. **Disconnect** releases the AirPods from Windows.
6. The other device can use the AirPods again.
7. If the native operation fails, PodShift still offers the Windows Bluetooth Settings fallback.

Include your Windows version, AirPods model, and Bluetooth adapter/laptop model in the PR when relevant.

## Style

- Keep the application dependency-light.
- Prefer clear C# over clever C#.
- Keep native interop isolated in the Bluetooth service layer.
- User-facing errors should explain what happened and leave a usable fallback path.
- Avoid claiming success before Windows actually reports the requested connection state.

## Pull requests

A good PR description should include:

- what changed;
- why it changed;
- how it was tested;
- hardware/Windows details when Bluetooth behavior is involved;
- screenshots for visible UI changes.

By contributing, you agree that your contribution may be distributed under the repository's MIT License.
