using DataLibrary.Contexts;
using DataLibrary.Entities;
using DataLibrary.Models;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Text;

namespace DataLibrary.Services;

public class DeviceManager
{
    private DeviceClient _deviceClient = null!;
    public bool IsSendingAllowed { get; set; } = true;
    public bool IsConfigured;
    public bool MethodRegistered;
    private int _telemetryInterval = 5000; //default is set to 5 seconds
    private readonly IServiceProvider? _serviceProvider;
    public readonly DataContext _dbcontext = null!;

    public DeviceManager(string url, string deviceId)
    {
        _dbcontext = new DataContext();

        Task.FromResult(ConfigureDevice(url, deviceId));

    }
    public async Task ConfigureDevice(string url, string deviceId)
    {
        try
        {
            var deviceConfig = await _dbcontext.DeviceConfiguration.FirstOrDefaultAsync();

            if (deviceConfig is null)
            {
                using var http = new HttpClient();
                var result = await http.PostAsync(url,
                    new StringContent(JsonConvert.SerializeObject(new { deviceId = deviceId })));

                deviceConfig ??= new DeviceConfig();

                if (result.IsSuccessStatusCode)
                {
                    var connectionString = await result.Content.ReadAsStringAsync();
                    deviceConfig.ConnectionString = connectionString;
                    deviceConfig.DeviceId = deviceId;

                    await _dbcontext.DeviceConfiguration.AddAsync(deviceConfig);
                    await _dbcontext.SaveChangesAsync();
                }
            }

            _deviceClient = DeviceClient.CreateFromConnectionString(deviceConfig.ConnectionString, TransportType.Mqtt);
            IsConfigured = true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
            IsConfigured = false;
        }
    }
    public async Task UpdateTwinPropsAsync(TwinCollection twinCollection)
    {
        if (IsConfigured)
            await _deviceClient!.UpdateReportedPropertiesAsync(twinCollection);
    }
    public async Task SendDataAsync(string payload)
    {
        if (IsConfigured)
        {
            var messageToCloud = new Message(Encoding.UTF8.GetBytes(payload));
            await _deviceClient!.SendEventAsync(messageToCloud);
        }
    }
    public async Task SendTelemetryDataAsync(string payload, int interval)
    {
        if (IsConfigured)
        {
            var messageToCloud = new Message(Encoding.UTF8.GetBytes(payload));
            await _deviceClient!.SendEventAsync(messageToCloud);
        }
        await Task.Delay(_telemetryInterval);
    }
    public async Task RegisterDirectMethodsToCloud()
    {
        if (IsConfigured)
            await _deviceClient!.SetMethodDefaultHandlerAsync(MethodResponseCallback, null!);
    }
    private async Task<MethodResponse> MethodResponseCallback(MethodRequest req, object userContext)
    {
        var reponseToCloud = new ResponseMessage { Message = $"Method: {req.Name.ToUpper()} is executed." };
        var twinCollection = new TwinCollection();

        try
        {
            switch (req.Name.ToLower())
            {
                case "start":
                    IsSendingAllowed = true;
                    twinCollection["state"] = "active";
                    await _deviceClient!.UpdateReportedPropertiesAsync(twinCollection);
                    break;

                case "stop":
                    IsSendingAllowed = false;
                    twinCollection["state"] = "inactive";
                    await _deviceClient!.UpdateReportedPropertiesAsync(twinCollection);
                    break;

                case "telemetry":
                    _telemetryInterval = Convert.ToInt32(Encoding.UTF8.GetString(req.Data))!;
                    break;

                default:
                    reponseToCloud.Message = $"Method: {req.Name.ToUpper()} is not found.";
                    return new MethodResponse(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(reponseToCloud)), 404);
            }

            return new MethodResponse(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(reponseToCloud)), 200);
        }
        catch (Exception e)
        {
            reponseToCloud.Message = $"Error occured: {e.Message}";
            return new MethodResponse(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(reponseToCloud)), 400);
        }
    }
    private async Task<Twin> ReadDesiredPropertiesAsync()
    {
        var twin = await _deviceClient.GetTwinAsync();
        if (twin is not null)
            return twin;

        return null!;
    }
}
