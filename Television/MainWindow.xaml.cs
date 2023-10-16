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
    public MainWindow(DeviceManager deviceManager, NetworkService networkService)
    {
        InitializeComponent();

        _deviceManager = deviceManager;
        _networkService = networkService;

        Task.WhenAll(GetConnectionStatusAsync(),
            UpdateTwin(),
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
                state = "ON";
                DeviceState.Text = $"{state}";
            }
            else
            {
                device.Stop();
                MusicIcon.Visibility = Visibility.Collapsed;
                state = "OFF";
                DeviceState.Text = $"{state}";
            }

            await Task.Delay(1000);
        }

    }
    private async Task UpdateTwin()
    {
        var twincollection = new TwinCollection();
        twincollection["deviceType"] = "wpf";
        twincollection["deviceName"] = "television";
        twincollection["location"] = "lounge";

        await _deviceManager.UpdateTwinPropsAsync(twincollection);
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

                await _deviceManager.SendTelemetryDataAsync(JsonConvert.SerializeObject(payload), 5000);
                CloudMessage.Text = $"Volume: {payload.Volume}\nBattery: {payload.Battery}\nChannel: {payload.Channel}\nTime: {payload.Time}";
            }
        }
    }
}
