using System.Diagnostics;
using System.Net;
using AzureFunctions.Models;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Azure.Devices;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace AzureFunctions.Methods;

public class GetAllDevicesFromCloud
{
    public readonly RegistryManager _registryManager;
    public readonly IConfiguration _configuration;
    public string? IotHubConnectionString;
    public GetAllDevicesFromCloud(IConfiguration configuration)
    {
        _configuration = configuration;
        IotHubConnectionString = _configuration.GetConnectionString("iotHub");
        _registryManager = RegistryManager.CreateFromConnectionString(IotHubConnectionString);
    }

    [Function("GetAllDevicesFromCloud")]
    public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "get", Route = "GetAllDevices")] HttpRequestData req)
    {
        try
        {
            var queryResult = _registryManager.CreateQuery("select * from devices");
            var deviceTwinList = new List<Twin>();

            if (queryResult.HasMoreResults)
                foreach (var item in await queryResult.GetNextAsTwinAsync())
                    deviceTwinList.Add(item);

            return MethodResponseMessage.CreateReponseMessage(req, HttpStatusCode.OK, JsonConvert.SerializeObject(deviceTwinList));
        }
        catch (Exception ex) { Debug.WriteLine(ex.Message); }

        return MethodResponseMessage.CreateReponseMessage(req, HttpStatusCode.BadRequest, "Bad Request");
    }
}
