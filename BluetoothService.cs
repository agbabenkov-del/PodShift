using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Enumeration;

namespace PodShift;

public enum PodConnectionStatus
{
    Unknown,
    Connected,
    Disconnected
}

public sealed record BluetoothDeviceInfo(string Id, string Name);

public sealed record BluetoothActionResult(
    PodConnectionStatus Status,
    string Message);

public sealed class BluetoothService
{
    public async Task<BluetoothDeviceInfo?> FindLikelyAirPodsAsync()
    {
        var selector = BluetoothDevice.GetDeviceSelectorFromPairingState(true);
        var devices = await DeviceInformation.FindAllAsync(selector);

        var candidate = devices
            .Where(d => !string.IsNullOrWhiteSpace(d.Name))
            .OrderByDescending(d => ScoreName(d.Name))
            .FirstOrDefault(d => ScoreName(d.Name) > 0);

        return candidate is null
            ? null
            : new BluetoothDeviceInfo(candidate.Id, candidate.Name);
    }

    public async Task<PodConnectionStatus> GetStatusAsync(string deviceId)
    {
        var device = await BluetoothDevice.FromIdAsync(deviceId);
        if (device is null)
            return PodConnectionStatus.Unknown;

        return device.ConnectionStatus == Windows.Devices.Bluetooth.BluetoothConnectionStatus.Connected
            ? PodConnectionStatus.Connected
            : PodConnectionStatus.Disconnected;
    }

    public async Task<BluetoothActionResult> TryConnectAsync(string deviceId)
    {
        var before = await GetStatusAsync(deviceId);

        if (before == PodConnectionStatus.Connected)
        {
            return new BluetoothActionResult(
                PodConnectionStatus.Connected,
                "AirPods уже подключены.");
        }

        OpenBluetoothSettings();

        return new BluetoothActionResult(
            PodConnectionStatus.Disconnected,
            "Открыл настройки Bluetooth. В MVP 0.1 прямое подключение ещё не реализовано.");
    }

    public async Task<BluetoothActionResult> TryDisconnectAsync(string deviceId)
    {
        OpenBluetoothSettings();

        return new BluetoothActionResult(
            await GetStatusAsync(deviceId),
            "Открыл настройки Bluetooth. Прямое отключение добавим на следующем этапе.");
    }

    private static void OpenBluetoothSettings()
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = "ms-settings:bluetooth",
            UseShellExecute = true
        });
    }

    private static int ScoreName(string name)
    {
        var n = name.ToLowerInvariant();

        if (n.Contains("airpods pro")) return 100;
        if (n.Contains("airpods max")) return 95;
        if (n.Contains("airpods")) return 90;
        if (n.Contains("beats")) return 30;

        return 0;
    }
}
