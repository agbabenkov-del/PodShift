using System;
using System.IO;
using System.Text.Json;
using System.Windows;

namespace PodShift;

public enum AppSkin
{
    Default,
    Retro95
}

public static class ThemeService
{
    private static ResourceDictionary? _activeDictionary;

    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "PodShift",
        "settings.json");

    public static AppSkin CurrentSkin { get; private set; } = AppSkin.Default;

    public static void Initialize()
    {
        Apply(LoadSkin());
    }

    public static bool SelectSkin(AppSkin skin)
    {
        Apply(skin);
        return SaveSkin(skin);
    }

    private static void Apply(AppSkin skin)
    {
        if (!Enum.IsDefined(typeof(AppSkin), skin))
            skin = AppSkin.Default;

        // One dictionary is replaced in place; the window and Bluetooth selection stay alive.
        var dictionary = new ResourceDictionary
        {
            Source = new Uri(
                $"/PodShift;component/Themes/{skin}.xaml",
                UriKind.Relative)
        };
        var dictionaries = Application.Current.Resources.MergedDictionaries;

        if (_activeDictionary is null)
            dictionaries.Add(dictionary);
        else
            dictionaries[dictionaries.IndexOf(_activeDictionary)] = dictionary;

        _activeDictionary = dictionary;
        CurrentSkin = skin;
    }

    private static AppSkin LoadSkin()
    {
        try
        {
            var settings = JsonSerializer.Deserialize<SkinSettings>(File.ReadAllText(SettingsPath));
            return Enum.TryParse<AppSkin>(settings?.Skin, out var skin)
                   && Enum.IsDefined(typeof(AppSkin), skin)
                ? skin
                : AppSkin.Default;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            // Missing, malformed, or inaccessible preferences must not stop the app.
            return AppSkin.Default;
        }
    }

    private static bool SaveSkin(AppSkin skin)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
            var temporaryPath = SettingsPath + ".tmp";
            File.WriteAllText(temporaryPath, JsonSerializer.Serialize(new SkinSettings { Skin = skin.ToString() }));
            File.Move(temporaryPath, SettingsPath, overwrite: true);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // The selected skin still works for this session when the folder is read-only.
            return false;
        }
    }

    public sealed class SkinSettings
    {
        public string? Skin { get; set; }
    }
}
