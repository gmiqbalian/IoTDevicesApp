using DataLibrary.Services;
using Microsoft.Azure.Devices.Shared;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace WashingMachine
{
    public partial class MainWindow : Window
    {
        private readonly NetworkService _networkService;
        private readonly DeviceManager _deviceManager;
        private readonly TwinCollection _twinCollection;
        private readonly Timer _timer;
        public MainWindow(NetworkService networkService, DeviceManager deviceManager)
        {
            InitializeComponent();

            _networkService = networkService;
            _deviceManager = deviceManager;
            _twinCollection = new TwinCollection();


            Task.WhenAll(GetConnectionStatusAsync(),
                StartDevice(),
                UpdateTwin(),
                SendTelemetryDataToCloud(),
                ToggleDeviceState(),
                SaveLatestMessageToTwin());

            _timer = new Timer(3000);
            _timer.Elapsed += async (s, e) => await SaveLatestMessageToTwin();
            _timer.Start();

        }
        private async Task StartDevice()
        {
            await _deviceManager.ConfigureDevice("washingmachine");
            await _deviceManager.RegisterDirectMethodsToCloud();
        }
        private async Task ToggleDeviceState()
        {
            Storyboard device = (Storyboard)this.FindResource("MachineStoryboard");
            while (true)
            {
                if (_deviceManager.IsSendingAllowed)
                {
                    device.Begin();
                    await _deviceManager.UpdateTwinPropsAsync(_twinCollection["state"] = "ON");
                    DeviceState.Text = "ON";
                }
                else
                {
                    device.Stop();
                    await _deviceManager.UpdateTwinPropsAsync(_twinCollection["state"] = "OFF");
                    DeviceState.Text = "OFF";
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
            _twinCollection["deviceType"] = "WPF";
            _twinCollection["deviceName"] = "WashingMachine";
            _twinCollection["location"] = "Washroom";

            await _deviceManager.UpdateTwinPropsAsync(_twinCollection);
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

                    await _deviceManager.SendTelemetryDataAsync(JsonConvert.SerializeObject(payload));
                    CloudMessage.Text = $"WaterLevel: {payload.WaterLevel}\nWaterTemp: {payload.WaterTemp}\nTime: {payload.Time}";
                }
            }
        }
        private async Task SaveLatestMessageToTwin()
        {
            _twinCollection["lastestMessage"] = await _deviceManager.GetLatestMessageFromCloudAsync("washingmachine");
            await _deviceManager.UpdateTwinPropsAsync(_twinCollection);
        }
    }
}

