using System;
using System.IO;
using System.Text.Json;

namespace PodShift;

/// <summary>
/// Loads and saves <see cref="AppSettings"/> as JSON in %LOCALAPPDATA%\PodShift\settings.json.
/// Missing, unreadable or corrupt files fall back to defaults; save failures are ignored.
/// </summary>
public static class SettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private static string SettingsDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "PodShift");

    private static string SettingsPath => Path.Combine(SettingsDirectory, "settings.json");

    public static AppSettings Load()
    {
        try
        {
            if (!File.Exists(SettingsPath))
                return new AppSettings();

            var json = File.ReadAllText(SettingsPath);
            var settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);
            return Sanitize(settings);
        }
        catch (Exception)
        {
            return new AppSettings();
        }
    }

    public static void Save(AppSettings settings)
    {
        try
        {
            Directory.CreateDirectory(SettingsDirectory);

            var json = JsonSerializer.Serialize(Sanitize(settings), JsonOptions);
            var tempPath = SettingsPath + ".tmp";

            File.WriteAllText(tempPath, json);
            File.Move(tempPath, SettingsPath, overwrite: true);
        }
        catch (Exception)
        {
        }
    }

    private static AppSettings Sanitize(AppSettings? settings)
    {
        settings ??= new AppSettings();

        if (settings.Left is double left && !double.IsFinite(left))
            settings.Left = null;

        if (settings.Top is double top && !double.IsFinite(top))
            settings.Top = null;

        if (settings.Left is null || settings.Top is null)
        {
            settings.Left = null;
            settings.Top = null;
        }

        return settings;
    }
}
