using System;
using System.Globalization;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;

namespace PodShift;

/// <summary>
/// Reads the battery percentage that Windows exposes for a Bluetooth device.
/// This deliberately uses the Windows device property first instead of touching
/// the working A2DP switching path in BluetoothService.
/// </summary>
public sealed class BatteryService
{
    private const string BatteryLifeProperty = "System.Devices.BatteryLife";

    public async Task<int?> GetBatteryPercentAsync(string deviceId)
    {
        try
        {
            var info = await DeviceInformation.CreateFromIdAsync(
                deviceId,
                new[] { BatteryLifeProperty });

            if (info is null ||
                !info.Properties.TryGetValue(BatteryLifeProperty, out var raw) ||
                raw is null)
            {
                return null;
            }

            var value = Convert.ToInt32(raw, CultureInfo.InvariantCulture);

            // Windows uses 101 for an unknown battery state on this property.
            return value is >= 0 and <= 100 ? value : null;
        }
        catch
        {
            // Battery telemetry is optional. Failure must never affect switching.
            return null;
        }
    }
}
