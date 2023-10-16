using DataLibrary.Services;
using Microsoft.Azure.Devices.Shared;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace WashingMachine;

public partial class MainWindow : Window
{
    private readonly NetworkService _networkService;
    private readonly DeviceManager _deviceManager;
    public MainWindow(NetworkService networkService, DeviceManager deviceManager)
    {
        InitializeComponent();

        _networkService = networkService;
        _deviceManager = deviceManager;

        Task.WhenAll(GetConnectionStatusAsync(),
                UpdateTwin(),
                _deviceManager.RegisterDirectMethodsToCloud(),
                SendTelemetryDataToCloud(),
                ToggleDeviceState());
    }

    private async Task ToggleDeviceState()
    {
        Storyboard device = (Storyboard)this.FindResource("MachineStoryboard");
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
    private async Task UpdateTwin()
    {
        var twincollection = new TwinCollection();
        twincollection["deviceType"] = "wpf";
        twincollection["deviceName"] = "washingmachine";
        twincollection["location"] = "washroom";

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
                    WaterLevel = "5l",
                    WaterTemp = "30",
                    Time = DateTime.Now.ToString("HH:mm:ss")
                };

                await _deviceManager.SendTelemetryDataAsync(JsonConvert.SerializeObject(payload), 5000);
                CloudMessage.Text = $"WaterLevel: {payload.WaterLevel}\nWaterTemp: {payload.WaterTemp}\nTime: {payload.Time}";
            }
        }
    }
}
