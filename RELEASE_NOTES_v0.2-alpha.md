# v0.2-alpha

PodShift now performs real one-click AirPods switching on Windows instead of only sending you to Bluetooth Settings.

## Highlights

- One-click A2DP connect to Windows.
- One-click A2DP disconnect.
- Uses the real local Bluetooth radio handle for native service operations.
- Waits for Windows to confirm connection state before reporting success.
- Keeps Windows Bluetooth Settings as a fallback when the native path fails.
- Responsive window sizing fixes clipped lower controls and long AirPods names.
- Self-contained Windows x64 build: no separate .NET installation required.

## Real-world validation

The v0.2 path has been confirmed on a real setup: AirPods connected to an iPhone were transferred to Windows from PodShift without opening Bluetooth Settings, taking roughly 10 seconds. Disconnect also behaved as intended.

## Download

Download `PodShift-win-x64.zip`, extract it, and run `PodShift.exe`.

AirPods must already have been paired with Windows once.

## Notes

This is an alpha prerelease. Bluetooth behavior can differ across adapters, drivers, Windows versions, and AirPods models. The executable is currently unsigned, so Windows SmartScreen may show an unknown-publisher warning.

Please report successful and failed hardware combinations through GitHub Issues.
