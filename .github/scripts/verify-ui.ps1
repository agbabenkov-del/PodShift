param([Parameter(Mandatory = $true)][string]$OutputPath)
$ErrorActionPreference = 'Stop'
$taskDirectory = Join-Path $env:RUNNER_TEMP ('podshift-ui-' + [guid]::NewGuid())
New-Item -ItemType Directory -Path $taskDirectory -Force | Out-Null
$referencePath = [System.Security.SecurityElement]::Escape((Join-Path $env:GITHUB_WORKSPACE 'PodShift.csproj'))
@"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0-windows10.0.19041.0</TargetFramework>
    <UseWPF>true</UseWPF>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup><ProjectReference Include="$referencePath" /></ItemGroup>
</Project>
"@ | Set-Content (Join-Path $taskDirectory 'UiCheck.csproj') -Encoding utf8
@'
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shell;
using System.Windows.Threading;
using PodShift;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        try
        {
            Directory.CreateDirectory(args[0]);
            var app = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
            var preferences = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "PodShift", "settings.json");
            if (File.Exists(preferences))
                File.Delete(preferences);
            ThemeService.Initialize();
            Require(ThemeService.CurrentSkin == AppSkin.Default, "First launch must use Default.");
            var window = new MainWindow();
            window.Show();
            Settle(window);

            var selector = (ComboBox)window.FindName("SkinSelector");
            var name = (TextBlock)window.FindName("DeviceNameText");
            var status = (TextBlock)window.FindName("StatusText");
            var connect = (Button)window.FindName("ConnectButton");
            var disconnect = (Button)window.FindName("DisconnectButton");
            CheckLayout(window);
            Capture(window, args[0], "default");

            selector.SelectedIndex = 1;
            Settle(window);
            Require(ThemeService.CurrentSkin == AppSkin.Retro95, "Selector must apply Retro95.");
            Require(window.WindowStyle == WindowStyle.None, "Retro caption must replace native caption.");
            Require(WindowChrome.GetWindowChrome(window) != null, "Retro caption must support window dragging.");
            CheckLayout(window);
            Capture(window, args[0], "retro95");

            name.Text = "AirPods Pro (очень длинное имя устройства) #2 - Find My";
            status.Text = "Подключены к Windows";
            connect.IsEnabled = false;
            disconnect.IsEnabled = true;
            Settle(window);
            CheckLayout(window);
            Require(name.ActualHeight > 30, "Long device names must wrap.");
            Capture(window, args[0], "retro95-long-name");

            selector.SelectedIndex = 0;
            Settle(window);
            Require(ReferenceEquals(name, window.FindName("DeviceNameText")), "Skin switch must retain controls.");
            Require(name.Text.EndsWith("Find My") && !connect.IsEnabled && disconnect.IsEnabled,
                "Skin switch must retain the displayed device and button state.");
            Require(window.WindowStyle == WindowStyle.SingleBorderWindow, "Default must restore native caption.");
            Require(WindowChrome.GetWindowChrome(window) == null, "Default must clear retro window chrome.");
            CheckLayout(window);
            Capture(window, args[0], "default-long-name");

            var normalHeight = window.ActualHeight;
            status.Text = string.Join(" ", new string[12].Select(_ =>
                "Не удалось прочитать статус устройства. Повтори поиск или проверь настройки Bluetooth."));
            Settle(window);
            Require(window.ActualHeight > normalHeight, "Window must grow for long messages.");
            CheckLayout(window);
            Capture(window, args[0], "default-long-message");

            selector.SelectedIndex = 1;
            Settle(window);
            CheckLayout(window);
            ThemeService.Initialize();
            Require(ThemeService.CurrentSkin == AppSkin.Retro95, "Saved preference must reload.");
            File.WriteAllText(preferences, "{ invalid json");
            ThemeService.Initialize();
            Require(ThemeService.CurrentSkin == AppSkin.Default, "Invalid settings must fall back to Default.");
            File.WriteAllText(preferences, "{\"Skin\":\"Unsupported\"}");
            ThemeService.Initialize();
            Require(ThemeService.CurrentSkin == AppSkin.Default, "Unknown skins must fall back to Default.");
            window.Close();
            app.Shutdown();
            Console.WriteLine("PASS: both skins, live switching, long names/messages, layout bounds, and preferences.");
            return 0;
        }
        catch (Exception error)
        {
            Console.Error.WriteLine(error);
            return 1;
        }
    }

    private static void Settle(Window window)
    {
        window.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
        window.UpdateLayout();
    }

    private static void CheckLayout(Window window)
    {
        var root = (FrameworkElement)window.Content;
        foreach (var key in new[] { "FindButton", "ConnectButton", "DisconnectButton", "SkinSelector", "DeviceNameText", "StatusText" })
        {
            var control = (FrameworkElement)window.FindName(key);
            var bounds = control.TransformToAncestor(root).TransformBounds(new Rect(control.RenderSize));
            Require(bounds.Top >= 0 && bounds.Left >= 0 &&
                    bounds.Bottom <= root.ActualHeight + 1 && bounds.Right <= root.ActualWidth + 1,
                key + " is outside the client area.");
        }
    }

    private static void Capture(Window window, string directory, string name)
    {
        var root = (FrameworkElement)window.Content;
        foreach (var scale in new[] { 1.0, 1.5, 2.0 })
        {
            var bitmap = new RenderTargetBitmap((int)Math.Ceiling(root.ActualWidth * scale),
                (int)Math.Ceiling(root.ActualHeight * scale), 96 * scale, 96 * scale, PixelFormats.Pbgra32);
            bitmap.Render(root);
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            using var stream = File.Create(Path.Combine(directory, $"{name}-{(int)(scale * 100)}.png"));
            encoder.Save(stream);
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}
'@ | Set-Content (Join-Path $taskDirectory 'Program.cs') -Encoding utf8
dotnet run --project (Join-Path $taskDirectory 'UiCheck.csproj') --configuration Release -- $OutputPath
if ($LASTEXITCODE -ne 0) { throw "Windows UI verification failed with exit code $LASTEXITCODE" }
