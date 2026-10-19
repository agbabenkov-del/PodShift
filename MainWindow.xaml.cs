using System;
using System.Threading.Tasks;
using System.Windows;

namespace PodShift;

public partial class MainWindow : Window
{
    private readonly BluetoothService _bluetoothService = new();
    private BluetoothDeviceInfo? _selectedDevice;
    private PodConnectionStatus _lastStatus = PodConnectionStatus.Unknown;
    private bool _isBusy;

    public MainWindow()
    {
        InitializeComponent();
        Activated += async (_, _) => await RefreshStatusQuietlyAsync();
    }

    private async void FindButton_Click(object sender, RoutedEventArgs e)
    {
        await RunBusy(async () =>
        {
            StatusText.Text = "Ищу сохранённые AirPods...";
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
            var progress = new Progress<string>(message => StatusText.Text = message);
            StatusText.Text = "Запускаю one-click подключение...";

            var result = await _bluetoothService.TryConnectAsync(
                _selectedDevice.Id,
                progress);

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
            var progress = new Progress<string>(message => StatusText.Text = message);
            StatusText.Text = "Отключаю AirPods от Windows...";

            var result = await _bluetoothService.TryDisconnectAsync(
                _selectedDevice.Id,
                progress);

            StatusText.Text = result.Message;
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
            // Фоновое обновление не должно мешать работе окна.
        }
    }

    private void UpdateUi(PodConnectionStatus status, bool keepMessage = false)
    {
        _lastStatus = status;

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
        _isBusy = true;

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
            _isBusy = false;
            FindButton.IsEnabled = true;
            await RefreshButtonsQuietlyAsync();
        }
    }

    private async Task RefreshButtonsQuietlyAsync()
    {
        if (_selectedDevice is null)
        {
            ConnectButton.IsEnabled = false;
            DisconnectButton.IsEnabled = false;
            return;
        }

        try
        {
            var status = await _bluetoothService.GetStatusAsync(_selectedDevice.Id);
            _lastStatus = status;
            ConnectButton.IsEnabled = status != PodConnectionStatus.Connected;
            DisconnectButton.IsEnabled = status == PodConnectionStatus.Connected;
        }
        catch
        {
            ConnectButton.IsEnabled = true;
            DisconnectButton.IsEnabled = false;
        }
    }
}
