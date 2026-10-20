# Changelog

All notable changes to PodShift are documented here.

## [Unreleased] - v0.3-alpha

### Added
- Compact frameless desktop widget (about 300 × 160) with a single primary action button: «Найти AirPods», «Подключить к ПК» or «Отключить».
- Drag-to-move, remembered widget position, and clamping to a visible work area after monitor/layout changes.
- Optional «Поверх окон» (always on top) toggle, remembered between launches.
- Tray icon: double-click to show, menu with «Показать PodShift», connect/disconnect, «Поверх окон» and «Выход».
- Close (X) hides the widget to the tray instead of quitting; «Выход» in the tray menu exits.
- Settings stored in `%LOCALAPPDATA%\PodShift\settings.json`, with safe fallback to defaults.
- Automatic AirPods search at launch.

### Changed
- The primary button is disabled while a Bluetooth operation is running.
- Version metadata bumped to 0.3.0-alpha.

### Unchanged
- Bluetooth switching logic in `BluetoothService.cs` is untouched.

## [v0.2-alpha] - 2026-10-20

### Added
- Experimental one-click AirPods connect to Windows via A2DP service reset.
- One-click disconnect by disabling the A2DP stereo service.
- Native Bluetooth radio discovery and real radio handle usage.
- Progress messages while switching.
- Automatic connection-state refresh when the app becomes active again.
- Responsive window sizing so long device names and lower controls are not clipped.
- Self-contained Windows x64 build through GitHub Actions.
- Automated prerelease packaging with ZIP and SHA-256 checksum.

### Changed
- AirPods selection now focuses only on AirPods-family names instead of also treating Beats as candidates.
- The first implementation is intentionally A2DP-only; Hands-Free mode is not toggled.

### Verified
- Real-world test confirmed AirPods can move from iPhone to Windows in roughly 10 seconds without opening Windows Bluetooth Settings.
- Disconnect was also confirmed to work as intended on the same setup.

## [v0.1] - 2026-10-19

### Added
- Initial WPF application shell.
- Paired AirPods discovery.
- Device name and connection status display.
- Windows Bluetooth Settings fallback.
- GitHub Actions Windows build.
