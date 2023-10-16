using System.Threading.Tasks;
using System;
using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Media;
using DataLibrary.Services;
using Newtonsoft.Json;
using Microsoft.Azure.Devices.Shared;

namespace Television;

public partial class MainWindow : Window
{
    private readonly DeviceManager _deviceManager;
    private readonly NetworkService _networkService;
    private readonly TwinCollection _twinCollection;
    public MainWindow(DeviceManager deviceManager, NetworkService networkService)
    {
        InitializeComponent();

        _deviceManager = deviceManager;
        _networkService = networkService;
        _twinCollection = new TwinCollection();

        Task.WhenAll(GetConnectionStatusAsync(),
            UpdateDeviceTwin(),
            _deviceManager.RegisterDirectMethodsToCloud(),
            SendTelemetryDataToCloud(),
            ToggleDeviceState());
    }
    private async Task ToggleDeviceState()
    {
        Storyboard device = (Storyboard)this.FindResource("TvStoryboard");
        while (true)
        {
            var state = string.Empty;
            if (_deviceManager.IsSendingAllowed)
            {
                device.Begin();
                MusicIcon.Visibility = Visibility.Visible;
                await _deviceManager.UpdateTwinPropsAsync(_twinCollection["state"] = "ON");
                state = "ON";
                DeviceState.Text = $"{state}";
            }
            else
            {
                device.Stop();
                MusicIcon.Visibility = Visibility.Collapsed;
                state = "OFF";
                await _deviceManager.UpdateTwinPropsAsync(_twinCollection["state"] = "OFF");
                DeviceState.Text = $"{state}";
            }

            await Task.Delay(1000);
        }

    }
    private async Task UpdateDeviceTwin()
    {
        _twinCollection["deviceType"] = "WPF";
        _twinCollection["deviceName"] = "Television";
        _twinCollection["location"] = "Lounge";

        await _deviceManager.UpdateTwinPropsAsync(_twinCollection);
    }
    private async Task GetConnectionStatusAsync()
    {
        while (true)
        {
            var status = await _networkService.TestConnectivityAsync();
            ConnectivityStatus.Text = status;

            if (status.ToLower() == "connected")
                ConnectivityStatus.Background = Brushes.Green;
            else if (status.ToLower() == "disconnected")
                ConnectivityStatus.Background = Brushes.Red;

            await Task.Delay(1000);
        }
    }
    private async Task SendTelemetryDataToCloud()
    {
        while (true)
        {
            if (_deviceManager.IsSendingAllowed)
            {
                var payload = new
                {
                    Volume = "16db",
                    Battery = "20%",
                    Channel = "svt",
                    Time = DateTime.Now.ToString("HH:mm:ss")
                };

                await _deviceManager.SendTelemetryDataAsync(JsonConvert.SerializeObject(payload));
                CloudMessage.Text = $"Volume: {payload.Volume}\nBattery: {payload.Battery}\nChannel: {payload.Channel}\nTime: {payload.Time}";
            }
        }
    }
}
