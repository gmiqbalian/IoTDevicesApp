using DataLibrary.Contexts;
using DataLibrary.Entities;
using DataLibrary.Services;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Speakers
{
    public partial class MainWindow : Window
    {
        private readonly NetworkService _networkService;
        private readonly DeviceManager _deviceManager;
        private readonly TwinCollection _twinCollection;
        private readonly Timer _timer;
        public MainWindow(NetworkService networkService, DeviceManager deviceManager, IConfiguration configuration)
        {

            InitializeComponent();

            _networkService = networkService;
            _deviceManager = deviceManager;
            _twinCollection = new TwinCollection();
            _timer = new Timer();

            Task.WhenAll(GetConnectionStatusAsync(),
                StartDevice(),
                UpdateTwinToCloud(),
                SendTelemetryDataToCloud(),
                ToggleDeviceState(),
                SaveLatestMessageToTwin());

            _timer = new Timer(5000);
            _timer.Elapsed += async (s, e) => await SaveLatestMessageToTwin();
            _timer.Start();
        }
        private async Task StartDevice()
        {
            await _deviceManager.ConfigureDevice("speakers");
            await _deviceManager.RegisterDirectMethodsToCloud();
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
            _twinCollection["deviceType"] = "WPF";
            _twinCollection["deviceName"] = "Speakers";
            _twinCollection["location"] = "Lounge";

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
                        Volume = "16db",
                        Battery = "20%",
                        Time = DateTime.Now.ToString("HH:mm:ss")
                    };

                    await _deviceManager.SendTelemetryDataAsync(JsonConvert.SerializeObject(payload));
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
                else if (!_deviceManager.IsSendingAllowed)
                {
                    device.Stop();
                    state = "OFF";
                    DeviceState.Text = $"{state}";
                }

                await Task.Delay(1000);
            }
        }
        private async Task SaveLatestMessageToTwin()
        {
            _twinCollection["lastestMessage"] = await _deviceManager.GetLatestMessageFromCloudAsync("speakers");
            await _deviceManager.UpdateTwinPropsAsync(_twinCollection);
        }
    }
}

