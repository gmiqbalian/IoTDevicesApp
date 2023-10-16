using DataLibrary.Services;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Speakers;

public partial class MainWindow : Window
{
    private NetworkService _networkService;
    private DeviceManager _deviceManager;

    public MainWindow(NetworkService networkService, DeviceManager deviceManager, IConfiguration configuration)
    {

        InitializeComponent();

        _networkService = networkService;
        _deviceManager = deviceManager;

        Task.WhenAll(GetConnectionStatusAsync(),
            UpdateTwinToCloud(),
            _deviceManager.RegisterDirectMethodsToCloud(),
            SendTelemetryDataToCloud(),
            ToggleDeviceState());
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
    private async Task UpdateTwinToCloud()
    {
        var twincollection = new TwinCollection();
        twincollection["deviceType"] = "wpf";
        twincollection["deviceName"] = "speakers";
        twincollection["location"] = "lounge";

        await _deviceManager.UpdateTwinPropsAsync(twincollection);
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
                    Time = DateTime.Now.ToString("HH:mm:ss")
                };

                await _deviceManager.SendTelemetryDataAsync(JsonConvert.SerializeObject(payload), 10000);
                CloudMessage.Text = $"Volume: {payload.Volume}\nBattery: {payload.Battery}\nTime: {payload.Time}";
            }
        }
    }
    private async Task ToggleDeviceState()
    {
        Storyboard device = (Storyboard)FindResource("SpeakerStoryboard");
        while (true)
        {
            var state = string.Empty;
            if (_deviceManager.IsSendingAllowed)
            {
                device.Begin();
                state = "ON";
                DeviceState.Text = $"{state}";
            }
            else
            {
                device.Stop();
                state = "OFF";
                DeviceState.Text = $"{state}";
            }

            await Task.Delay(1000);
        }
    }
}
