using DataLibrary.Contexts;
using DataLibrary.Entities;
using DataLibrary.Models;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Text;
using System.Text.Json.Nodes;

namespace DataLibrary.Services;

public class DeviceManager
{
    private DeviceClient _deviceClient = null!;
    public bool IsSendingAllowed { get; set; } = true;
    public bool IsConfigured;
    public bool MethodRegistered;
    private int _telemetryInterval = 10000; //default is set to 10 seconds
    private readonly IServiceProvider? _serviceProvider;
    public readonly DataContext _dbcontext = null!;
    private readonly IConfiguration _configuration;
    public DeviceManager(DataContext dbcontext, IConfiguration configuration)
    {
        _dbcontext = dbcontext;
        _configuration = configuration;
    }
    public async Task ConfigureDevice(string deviceId)
    {
        try
        {
            var device = _dbcontext.DeviceConfig
                .FirstOrDefault(x => x.Id == deviceId);

            if (device is null)
            {
                using var http = new HttpClient();
                var result = await http.PostAsync(_configuration.GetConnectionString("apiurl"),
                    new StringContent(JsonConvert.SerializeObject(new { deviceId = deviceId })));

                device ??= new DeviceConfig();

                if (result.IsSuccessStatusCode)
                {
                    var connectionString = await result.Content.ReadAsStringAsync();
                    device.ConnectionString = connectionString;
                    device.Id = deviceId;
                }
                if (_dbcontext.DeviceConfig.Any(d => d.Id == device.Id))
                    _dbcontext.DeviceConfig.Update(device);
                else
                    await _dbcontext.DeviceConfig.AddAsync(device);

                await _dbcontext.SaveChangesAsync();

            }

            _deviceClient = DeviceClient.CreateFromConnectionString(device!.ConnectionString, TransportType.Mqtt);
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
    public async Task SendTelemetryDataAsync(string payload)
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
        try
        {
            if (IsConfigured)
                await _deviceClient!.SetMethodDefaultHandlerAsync(MethodResponseCallback, null!);
        }
        catch (Exception e) { Debug.WriteLine(e.Message); }
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
                    twinCollection["state"] = "ON";
                    await _deviceClient!.UpdateReportedPropertiesAsync(twinCollection);
                    break;

                case "stop":
                    IsSendingAllowed = false;
                    twinCollection["state"] = "OFF";
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
    public async Task<LatestCloudMessage> GetLatestMessageFromCloudAsync(string deviceId)
    {
        var requestUri = $"{_configuration.GetConnectionString("deviceMsgUrl")}&deviceId={deviceId}";

        using var httpClient = new HttpClient();

        var result = await httpClient.GetAsync(requestUri);
        var content = result.Content.ReadAsStringAsync().Result.ToString();
        var latestMsg = JsonConvert.DeserializeObject<LatestCloudMessage>(content);
        
        if (result.IsSuccessStatusCode)
            return latestMsg;

        return null!;
    }
}
