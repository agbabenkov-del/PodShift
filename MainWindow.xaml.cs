using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace PodShift;

public partial class MainWindow : Window
{
    private const string FindText = "Найти AirPods";
    private const string ConnectText = "Подключить к ПК";
    private const string DisconnectText = "Отключить";

    private readonly BluetoothService _bluetoothService = new();
    private readonly AppSettings _settings;
    private readonly TrayService _tray;

    private BluetoothDeviceInfo? _selectedDevice;
    private PodConnectionStatus _lastStatus = PodConnectionStatus.Unknown;
    private bool _isBusy;
    private bool _isExiting;

    public MainWindow()
    {
        InitializeComponent();

        _settings = SettingsService.Load();
        Topmost = _settings.Topmost;
        PinToggle.IsChecked = _settings.Topmost;

        _tray = new TrayService();
        _tray.SetTopmost(Topmost);
        _tray.ShowRequested += (_, _) => ShowWidget();
        _tray.ConnectToggleRequested += async (_, _) => await RunPrimaryActionAsync();
        _tray.TopmostToggleRequested += (_, _) => SetTopmost(!Topmost);
        _tray.ExitRequested += (_, _) => ExitApplication();

        SourceInitialized += (_, _) => RestorePosition();
        Loaded += async (_, _) => await FindAsync();
        Activated += async (_, _) => await RefreshStatusQuietlyAsync();
        Closing += MainWindow_Closing;

        Application.Current.SessionEnding += (_, _) => _isExiting = true;
    }

    private void RestorePosition()
    {
        double left;
        double top;

        if (_settings.Left is double savedLeft && _settings.Top is double savedTop)
        {
            left = savedLeft;
            top = savedTop;
        }
        else
        {
            var workArea = SystemParameters.WorkArea;
            left = workArea.Right - Width - 24;
            top = workArea.Bottom - Height - 24;
        }

        MoveInsideVisibleArea(left, top);
    }

    private void EnsureOnScreen() => MoveInsideVisibleArea(Left, Top);

    private void MoveInsideVisibleArea(double left, double top)
    {
        var dpi = VisualTreeHelper.GetDpi(this);
        var position = WindowPlacement.ClampToWorkArea(left, top, Width, Height, dpi);
        Left = position.X;
        Top = position.Y;
    }

    private void SavePosition()
    {
        if (WindowState != WindowState.Normal)
            return;

        _settings.Left = Left;
        _settings.Top = Top;
        SettingsService.Save(_settings);
    }

    private void SetTopmost(bool topmost)
    {
        Topmost = topmost;
        PinToggle.IsChecked = topmost;
        _tray.SetTopmost(topmost);

        _settings.Topmost = topmost;
        SettingsService.Save(_settings);
    }

    public void ShowWidget()
    {
        EnsureOnScreen();

        if (!IsVisible)
            Show();

        if (WindowState == WindowState.Minimized)
            WindowState = WindowState.Normal;

        Activate();
    }

    private void ExitApplication()
    {
        _isExiting = true;
        Application.Current.Shutdown();
    }

    private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        SavePosition();

        if (_isExiting)
        {
            _tray.Dispose();
            return;
        }

        e.Cancel = true;
        Hide();
    }

    private void Root_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState != MouseButtonState.Pressed)
            return;

        try
        {
            DragMove();
        }
        catch (InvalidOperationException)
        {
            return;
        }

        SavePosition();
    }

    private void PinToggle_Click(object sender, RoutedEventArgs e)
    {
        SetTopmost(PinToggle.IsChecked == true);
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private async void PrimaryButton_Click(object sender, RoutedEventArgs e)
    {
        await RunPrimaryActionAsync();
    }

    private Task RunPrimaryActionAsync()
    {
        if (_isBusy)
            return Task.CompletedTask;

        if (_selectedDevice is null)
            return FindAsync();

        return _lastStatus == PodConnectionStatus.Connected
            ? DisconnectAsync()
            : ConnectAsync();
    }

    private Task FindAsync()
    {
        return RunBusy(async () =>
        {
            SetStatus("Ищу сохранённые AirPods...");
            _selectedDevice = await _bluetoothService.FindLikelyAirPodsAsync();

            if (_selectedDevice is null)
            {
                SetDeviceName("AirPods не найдены");
                SetStatus("Сначала один раз спарь AirPods с Windows обычным способом, затем повтори поиск.");
                return;
            }

            SetDeviceName(_selectedDevice.Name);
            var status = await _bluetoothService.GetStatusAsync(_selectedDevice.Id);
            UpdateUi(status);
        });
    }

    private Task ConnectAsync()
    {
        var device = _selectedDevice;
        if (device is null)
            return Task.CompletedTask;

        return RunBusy(async () =>
        {
            var progress = new Progress<string>(SetStatus);
            SetStatus("Запускаю one-click подключение...");

            var result = await _bluetoothService.TryConnectAsync(device.Id, progress);

            SetStatus(result.Message);
            UpdateUi(result.Status, keepMessage: true);
        });
    }

    private Task DisconnectAsync()
    {
        var device = _selectedDevice;
        if (device is null)
            return Task.CompletedTask;

        return RunBusy(async () =>
        {
            var progress = new Progress<string>(SetStatus);
            SetStatus("Отключаю AirPods от Windows...");

            var result = await _bluetoothService.TryDisconnectAsync(device.Id, progress);

            SetStatus(result.Message);
            UpdateUi(result.Status, keepMessage: true);
        });
    }

    private async Task RefreshStatusQuietlyAsync()
    {
        if (_isBusy || _selectedDevice is null)
            return;

        try
        {
            var status = await _bluetoothService.GetStatusAsync(_selectedDevice.Id);
            if (status != _lastStatus)
                UpdateUi(status);
        }
        catch
        {
        }
    }

    private void UpdateUi(PodConnectionStatus status, bool keepMessage = false)
    {
        _lastStatus = status;

        if (!keepMessage)
        {
            SetStatus(status switch
            {
                PodConnectionStatus.Connected => "Подключены к Windows",
                PodConnectionStatus.Disconnected => "Не подключены к Windows",
                _ => "Статус подключения неизвестен"
            });
        }

        ApplyPrimaryState();
    }

    private void ApplyPrimaryState()
    {
        string? trayText = null;

        if (_selectedDevice is null)
        {
            PrimaryButton.Content = FindText;
        }
        else if (_lastStatus == PodConnectionStatus.Connected)
        {
            PrimaryButton.Content = DisconnectText;
            trayText = "Отключить AirPods";
        }
        else
        {
            PrimaryButton.Content = ConnectText;
            trayText = "Подключить AirPods к ПК";
        }

        PrimaryButton.IsEnabled = !_isBusy;
        _tray.SetConnectAction(trayText, enabled: !_isBusy);
    }

    private void SetStatus(string text)
    {
        StatusText.Text = text;
        StatusText.ToolTip = text;
    }

    private void SetDeviceName(string name)
    {
        DeviceNameText.Text = name;
        DeviceNameText.ToolTip = name;
    }

    private async Task RunBusy(Func<Task> action)
    {
        if (_isBusy)
            return;

        _isBusy = true;
        ApplyPrimaryState();

        try
        {
            await action();
        }
        catch (Exception ex)
        {
            SetStatus($"Ошибка: {ex.Message}");
        }
        finally
        {
            await RefreshLastStatusQuietlyAsync();

            _isBusy = false;
            ApplyPrimaryState();

            if (!IsVisible)
                _tray.ShowBalloon(StatusText.Text);
        }
    }

    private async Task RefreshLastStatusQuietlyAsync()
    {
        if (_selectedDevice is null)
            return;

        try
        {
            _lastStatus = await _bluetoothService.GetStatusAsync(_selectedDevice.Id);
        }
        catch
        {
            _lastStatus = PodConnectionStatus.Unknown;
        }
    }
}
