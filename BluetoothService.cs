using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
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
    private static readonly Guid A2dpSink = new("0000110B-0000-1000-8000-00805F9B34FB");

    private const uint BluetoothServiceDisable = 0x00;
    private const uint BluetoothServiceEnable = 0x01;
    private const uint ErrorSuccess = 0x00000000;
    private const uint EInvalidArg = 0x80070057;
    private const int ServiceResetDelayMs = 1000;

    public async Task<BluetoothDeviceInfo?> FindLikelyAirPodsAsync()
    {
        var selector = BluetoothDevice.GetDeviceSelectorFromPairingState(true);
        var devices = await DeviceInformation.FindAllAsync(selector);

        var candidate = devices
            .Where(d => !string.IsNullOrWhiteSpace(d.Name))
            .Select(d => new { Device = d, Score = ScoreName(d.Name) })
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .Select(x => x.Device)
            .FirstOrDefault();

        return candidate is null
            ? null
            : new BluetoothDeviceInfo(candidate.Id, candidate.Name);
    }

    public async Task<PodConnectionStatus> GetStatusAsync(string deviceId)
    {
        var device = await BluetoothDevice.FromIdAsync(deviceId);
        if (device is null)
            return PodConnectionStatus.Unknown;

        return device.ConnectionStatus == BluetoothConnectionStatus.Connected
            ? PodConnectionStatus.Connected
            : PodConnectionStatus.Disconnected;
    }

    public async Task<BluetoothActionResult> TryConnectAsync(
        string deviceId,
        IProgress<string>? progress = null)
    {
        var before = await GetStatusAsync(deviceId);
        if (before == PodConnectionStatus.Connected)
        {
            return new BluetoothActionResult(
                PodConnectionStatus.Connected,
                "AirPods уже подключены к Windows.");
        }

        progress?.Report("Получаю адрес AirPods...");
        var address = await GetAddressAsync(deviceId);
        if (address is null)
        {
            OpenBluetoothSettings();
            return new BluetoothActionResult(
                PodConnectionStatus.Unknown,
                "Не удалось получить Bluetooth-адрес AirPods. Открыл настройки Windows.");
        }

        progress?.Report("Сбрасываю стереопрофиль A2DP...");
        var nativeResult = await Task.Run(() => ResetA2dp(address.Value, progress));

        if (!nativeResult.Success)
        {
            OpenBluetoothSettings();
            return new BluetoothActionResult(
                await GetStatusAsync(deviceId),
                $"One-click не сработал: {nativeResult.Message} Открыл настройки Bluetooth.");
        }

        progress?.Report("A2DP включён. Жду подключения AirPods...");
        var after = await WaitForStatusAsync(
            deviceId,
            PodConnectionStatus.Connected,
            TimeSpan.FromSeconds(10));

        return after == PodConnectionStatus.Connected
            ? new BluetoothActionResult(after, "Подключено к Windows в один клик.")
            : new BluetoothActionResult(
                after,
                "Команда A2DP отправлена, но Windows не увидела подключение за 10 секунд. Возможно, AirPods удерживает другое устройство.");
    }

    public async Task<BluetoothActionResult> TryDisconnectAsync(
        string deviceId,
        IProgress<string>? progress = null)
    {
        var before = await GetStatusAsync(deviceId);
        if (before == PodConnectionStatus.Disconnected)
        {
            return new BluetoothActionResult(
                PodConnectionStatus.Disconnected,
                "AirPods уже отключены от Windows.");
        }

        progress?.Report("Получаю адрес AirPods...");
        var address = await GetAddressAsync(deviceId);
        if (address is null)
        {
            OpenBluetoothSettings();
            return new BluetoothActionResult(
                before,
                "Не удалось получить Bluetooth-адрес AirPods. Открыл настройки Windows.");
        }

        progress?.Report("Отключаю стереопрофиль A2DP...");
        var nativeResult = await Task.Run(() => DisableA2dp(address.Value));

        if (!nativeResult.Success)
        {
            OpenBluetoothSettings();
            return new BluetoothActionResult(
                await GetStatusAsync(deviceId),
                $"One-click отключение не сработало: {nativeResult.Message} Открыл настройки Bluetooth.");
        }

        progress?.Report("Команда отправлена. Жду отключения...");
        var after = await WaitForStatusAsync(
            deviceId,
            PodConnectionStatus.Disconnected,
            TimeSpan.FromSeconds(6));

        return after == PodConnectionStatus.Disconnected
            ? new BluetoothActionResult(after, "Отключено от Windows. AirPods свободны для iPhone.")
            : new BluetoothActionResult(
                after,
                "A2DP отключён, но Windows всё ещё показывает AirPods подключёнными.");
    }

    private static async Task<ulong?> GetAddressAsync(string deviceId)
    {
        var device = await BluetoothDevice.FromIdAsync(deviceId);
        return device?.BluetoothAddress;
    }

    private async Task<PodConnectionStatus> WaitForStatusAsync(
        string deviceId,
        PodConnectionStatus target,
        TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        var status = await GetStatusAsync(deviceId);

        while (status != target && DateTime.UtcNow < deadline)
        {
            await Task.Delay(500);
            status = await GetStatusAsync(deviceId);
        }

        return status;
    }

    private static NativeOperationResult ResetA2dp(
        ulong address,
        IProgress<string>? progress)
    {
        if (!TryOpenRadioForDevice(address, out var radioHandle, out var deviceInfo))
        {
            return new NativeOperationResult(
                false,
                "не найден Bluetooth-радиомодуль, которому известны эти AirPods.");
        }

        try
        {
            var service = A2dpSink;
            var disableResult = BluetoothSetServiceState(
                radioHandle,
                ref deviceInfo,
                ref service,
                BluetoothServiceDisable);

            progress?.Report("A2DP сброшен. Даю Windows секунду...");
            Thread.Sleep(ServiceResetDelayMs);

            service = A2dpSink;
            var enableResult = BluetoothSetServiceState(
                radioHandle,
                ref deviceInfo,
                ref service,
                BluetoothServiceEnable);

            if (!IsAcceptableServiceResult(enableResult))
            {
                return new NativeOperationResult(
                    false,
                    $"Windows вернула код {enableResult} при включении A2DP (код сброса: {disableResult}).");
            }

            return new NativeOperationResult(true, string.Empty);
        }
        finally
        {
            CloseHandle(radioHandle);
        }
    }

    private static NativeOperationResult DisableA2dp(ulong address)
    {
        if (!TryOpenRadioForDevice(address, out var radioHandle, out var deviceInfo))
        {
            return new NativeOperationResult(
                false,
                "не найден Bluetooth-радиомодуль, которому известны эти AirPods.");
        }

        try
        {
            var service = A2dpSink;
            var result = BluetoothSetServiceState(
                radioHandle,
                ref deviceInfo,
                ref service,
                BluetoothServiceDisable);

            return IsAcceptableServiceResult(result)
                ? new NativeOperationResult(true, string.Empty)
                : new NativeOperationResult(false, $"Windows вернула код {result} при отключении A2DP.");
        }
        finally
        {
            CloseHandle(radioHandle);
        }
    }

    private static bool TryOpenRadioForDevice(
        ulong address,
        out IntPtr radioHandle,
        out BluetoothDeviceInfoNative deviceInfo)
    {
        radioHandle = IntPtr.Zero;
        deviceInfo = CreateNativeDeviceInfo(address);

        var findParams = new BluetoothFindRadioParams
        {
            dwSize = (uint)Marshal.SizeOf<BluetoothFindRadioParams>()
        };

        var findHandle = BluetoothFindFirstRadio(ref findParams, out var candidateRadio);
        if (findHandle == IntPtr.Zero)
            return false;

        try
        {
            while (candidateRadio != IntPtr.Zero)
            {
                var candidateInfo = CreateNativeDeviceInfo(address);
                var getInfoResult = BluetoothGetDeviceInfo(candidateRadio, ref candidateInfo);

                if (getInfoResult == ErrorSuccess)
                {
                    radioHandle = candidateRadio;
                    deviceInfo = candidateInfo;
                    return true;
                }

                CloseHandle(candidateRadio);
                candidateRadio = IntPtr.Zero;

                if (!BluetoothFindNextRadio(findHandle, out candidateRadio))
                    break;
            }

            return false;
        }
        finally
        {
            BluetoothFindRadioClose(findHandle);
        }
    }

    private static BluetoothDeviceInfoNative CreateNativeDeviceInfo(ulong address)
    {
        return new BluetoothDeviceInfoNative
        {
            dwSize = (uint)Marshal.SizeOf<BluetoothDeviceInfoNative>(),
            Address = address,
            szName = string.Empty
        };
    }

    private static bool IsAcceptableServiceResult(uint result)
    {
        return result == ErrorSuccess || result == EInvalidArg;
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

        return 0;
    }

    private sealed record NativeOperationResult(bool Success, string Message);

    [StructLayout(LayoutKind.Sequential)]
    private struct BluetoothFindRadioParams
    {
        public uint dwSize;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct SystemTime
    {
        public ushort wYear;
        public ushort wMonth;
        public ushort wDayOfWeek;
        public ushort wDay;
        public ushort wHour;
        public ushort wMinute;
        public ushort wSecond;
        public ushort wMilliseconds;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct BluetoothDeviceInfoNative
    {
        public uint dwSize;
        public ulong Address;
        public uint ulClassofDevice;
        public int fConnected;
        public int fRemembered;
        public int fAuthenticated;
        public SystemTime stLastSeen;
        public SystemTime stLastUsed;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 248)]
        public string szName;
    }

    [DllImport("bthprops.cpl", SetLastError = true)]
    private static extern IntPtr BluetoothFindFirstRadio(
        ref BluetoothFindRadioParams pbtfrp,
        out IntPtr phRadio);

    [DllImport("bthprops.cpl", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool BluetoothFindNextRadio(
        IntPtr hFind,
        out IntPtr phRadio);

    [DllImport("bthprops.cpl", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool BluetoothFindRadioClose(IntPtr hFind);

    [DllImport("bthprops.cpl", SetLastError = true)]
    private static extern uint BluetoothGetDeviceInfo(
        IntPtr hRadio,
        ref BluetoothDeviceInfoNative pbtdi);

    [DllImport("bthprops.cpl", SetLastError = true)]
    private static extern uint BluetoothSetServiceState(
        IntPtr hRadio,
        ref BluetoothDeviceInfoNative pbtdi,
        ref Guid pGuidService,
        uint dwServiceFlags);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CloseHandle(IntPtr hObject);
}
