# PodShift v0.3 — Desktop Widget implementation brief

You are working in the repository `agbabenkov-del/PodShift` on branch `feature/v0.3-desktop-widget`.

## Context

PodShift is a small Windows utility written in C# / .NET 8 / WPF. The current release is `v0.2-alpha`.

The current Bluetooth switching logic already works on a real AirPods/iPhone/Windows setup. It can:

- find already-paired AirPods;
- show current connection state;
- connect A2DP stereo to Windows with one click;
- disconnect A2DP stereo with one click;
- fall back to Windows Bluetooth Settings when native switching fails.

Do not rewrite or replace the working Bluetooth implementation unless required to fix a regression caused by this feature.

Relevant files:

- `BluetoothService.cs` — working Bluetooth logic; preserve behavior.
- `MainWindow.xaml` — current 420 px WPF window.
- `MainWindow.xaml.cs` — current UI state and connect/disconnect handlers.
- `PodShift.csproj` — .NET 8 WPF project.
- `README.md` — current behavior and roadmap.

## Goal

Implement `PodShift v0.3` as a compact desktop widget while preserving all working v0.2 Bluetooth functionality.

The result should feel like a tiny daily-use Windows utility, not a settings dialog.

## Required UX

### Default widget

Create a compact frameless window approximately 280–320 px wide and 130–170 px tall.

It should show:

- app/device name;
- AirPods connection state;
- one primary action button;
- connected state: primary button says `Отключить`;
- disconnected state: primary button says `Подключить к ПК`;
- if no AirPods have been selected/found yet, show a clear `Найти AirPods` action.

Do not keep three large separate Find / Connect / Disconnect buttons in the default widget.

### Window behavior

Implement all of the following:

1. Frameless WPF window (`WindowStyle=None`).
2. Rounded visual container and restrained Windows-like appearance.
3. User can drag the widget by holding the header/background area.
4. Remember the last widget position across launches.
5. Prevent restoring completely off-screen after monitor/layout changes; clamp position to a visible work area.
6. Add an optional pin / `Always on top` toggle in the widget UI or context menu.
7. Persist the pin state across launches.
8. Closing the window with the X button should hide it to the tray, not terminate the app.
9. Provide a real `Выход` command from the tray menu that terminates PodShift.
10. Double-clicking the tray icon should restore/show the widget.
11. Tray menu should contain at minimum:
   - `Показать PodShift`
   - dynamic connect/disconnect action when a device is known
   - `Поверх окон` check/toggle
   - `Выход`

### Status behavior

Preserve and improve current status refresh behavior:

- refresh status when the widget is shown/activated;
- after connect/disconnect operation, update the visible state;
- do not freeze the UI during Bluetooth operations;
- prevent repeated primary-button clicks while an operation is in progress;
- retain useful error/progress messages without breaking layout.

### Settings persistence

Avoid adding a heavy dependency just for settings.

Preferred implementation:

- save a tiny JSON settings file under `%LOCALAPPDATA%\PodShift\settings.json`;
- persist at least `Left`, `Top`, and `Topmost`;
- handle missing/corrupt settings safely and fall back to defaults.

### Tray implementation

Prefer the built-in Windows Forms `NotifyIcon` from a WPF application rather than adding a third-party tray package, unless there is a strong technical reason not to.

If using `System.Windows.Forms.NotifyIcon`, update `PodShift.csproj` correctly (for example `UseWindowsForms`) while keeping WPF enabled.

Reuse the existing PodShift icon where practical.

## Architecture constraints

- Keep `BluetoothService.cs` focused on Bluetooth operations.
- Do not put all new widget, tray, and persistence logic into a single giant event-handler method.
- Small helper/service classes are welcome, for example:
  - `AppSettings.cs` / `SettingsService.cs`
  - `TrayService.cs`
- Keep the implementation understandable for a small open-source project.
- Do not introduce MVVM framework dependencies for this release.
- Do not introduce an installer in this change.
- Do not implement AirPods battery reporting in this change.
- Do not implement multiple-device selection in this change.
- Do not touch Hands-Free/microphone Bluetooth profiles.

## Regression requirements

The following v0.2 behavior must continue to work:

1. Find already-paired AirPods.
2. Correctly show connected/disconnected state.
3. One-click A2DP connect.
4. One-click A2DP disconnect.
5. Windows Settings fallback on native failure.
6. Long AirPods names must not break the UI.
7. Build remains self-contained for Windows x64 through the existing GitHub Actions workflow.

## Versioning and documentation

Update version metadata from `0.2.0-alpha` to `0.3.0-alpha`.

Update `README.md` so that it accurately describes:

- compact desktop widget;
- tray icon;
- remembered position;
- optional always-on-top mode;
- close-to-tray behavior;
- how to fully exit the app.

Do not claim functionality that is not actually implemented.

## Build / validation

Before finishing:

1. Run `dotnet build PodShift.csproj -c Release`.
2. If practical, run the same publish command used by the repository workflow.
3. Fix compiler errors and warnings introduced by your change.
4. Review the final diff for accidental Bluetooth changes.

## Deliverable

Implement the feature directly on `feature/v0.3-desktop-widget`.

When finished, provide a concise summary containing:

- files changed/added;
- key UX behavior implemented;
- build result;
- any manual Windows behaviors that still need testing on real hardware.

Do not merge to `main`.