using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace PodShift;

public partial class MainWindow : Window
{
    private readonly BluetoothService _bluetoothService = new();
    private BluetoothDeviceInfo? _selectedDevice;
    private bool _initializingSkin = true;

    public MainWindow()
    {
        InitializeComponent();
        SkinSelector.SelectedIndex = ThemeService.CurrentSkin == AppSkin.Retro95 ? 1 : 0;
        _initializingSkin = false;
    }

    private void SkinSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_initializingSkin)
            return;

        var skin = SkinSelector.SelectedIndex == 1 ? AppSkin.Retro95 : AppSkin.Default;
        var saved = ThemeService.SelectSkin(skin);
        SkinSelector.ToolTip = saved
            ? "Выбранный скин сохраняется после закрытия."
            : "Скин применён. Не удалось сохранить выбор: проверь доступ к папке настроек.";
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private async void FindButton_Click(object sender, RoutedEventArgs e)
    {
        await RunBusy(async () =>
        {
            StatusText.Text = "Ищу сохранённые Bluetooth-устройства...";
            _selectedDevice = await _bluetoothService.FindLikelyAirPodsAsync();

            if (_selectedDevice is null)
            {
                DeviceNameText.Text = "AirPods не найдены";
                StatusText.Text = "Сначала один раз спарь AirPods с Windows обычным способом, затем повтори поиск.";
                return;
            }

            DeviceNameText.Text = _selectedDevice.Name;
            var status = await _bluetoothService.GetStatusAsync(_selectedDevice.Id);
            UpdateUi(status);
        });
    }

    private async void ConnectButton_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedDevice is null)
            return;

        await RunBusy(async () =>
        {
            StatusText.Text = "Пытаюсь подключить...";
            var result = await _bluetoothService.TryConnectAsync(_selectedDevice.Id);
            StatusText.Text = result.Message;
            UpdateUi(result.Status, keepMessage: true);
        });
    }

    private async void DisconnectButton_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedDevice is null)
            return;

        await RunBusy(async () =>
        {
            StatusText.Text = "Пытаюсь отключить...";
            var result = await _bluetoothService.TryDisconnectAsync(_selectedDevice.Id);
            StatusText.Text = result.Message;
            UpdateUi(result.Status, keepMessage: true);
        });
    }

    private void UpdateUi(PodConnectionStatus status, bool keepMessage = false)
    {
        if (!keepMessage)
        {
            StatusText.Text = status switch
            {
                PodConnectionStatus.Connected => "Подключены к Windows",
                PodConnectionStatus.Disconnected => "Не подключены к Windows",
                _ => "Статус подключения неизвестен"
            };
        }

        ConnectButton.IsEnabled = status != PodConnectionStatus.Connected;
        DisconnectButton.IsEnabled = status == PodConnectionStatus.Connected;
    }

    private async Task RunBusy(Func<Task> action)
    {
        try
        {
            FindButton.IsEnabled = false;
            ConnectButton.IsEnabled = false;
            DisconnectButton.IsEnabled = false;
            await action();
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Ошибка: {ex.Message}";
        }
        finally
        {
            FindButton.IsEnabled = true;

            if (_selectedDevice is not null)
            {
                var status = await _bluetoothService.GetStatusAsync(_selectedDevice.Id);
                ConnectButton.IsEnabled = status != PodConnectionStatus.Connected;
                DisconnectButton.IsEnabled = status == PodConnectionStatus.Connected;
            }
        }
    }
}
