using System.Diagnostics;
using System.Net;
using AzureFunctions.Models;
using Microsoft.Azure.Devices;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace AzureFunctions.Methods;

public class AddDeviceToCloud
{
    public readonly RegistryManager _registryManager;
    public readonly IConfiguration _configuration;
    public string? IotHubConnectionString;
    public AddDeviceToCloud(IConfiguration configuration)
    {
        _configuration = configuration;
        IotHubConnectionString = _configuration.GetConnectionString("iotHub");
        _registryManager = RegistryManager.CreateFromConnectionString(IotHubConnectionString);
    }

    [Function("AddDeviceToCloud")]
    public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "post", Route = "AddDevice")] HttpRequestData req)
    {
        try
        {
            var deviceIdRequestBody = JsonConvert.DeserializeObject<DeviceFromHttpReq>(new StreamReader(req.Body).ReadToEnd());
            if (deviceIdRequestBody is not null)
            {
                var device = await _registryManager.GetDeviceAsync(deviceIdRequestBody.DeviceId);

                if (device is null)
                    device = await _registryManager.AddDeviceAsync(new Device(deviceIdRequestBody.DeviceId));

                var deviceConnectionString = $"{IotHubConnectionString!.Split(";")[0]};DeviceId={device.Id};SharedAccessKey={device.Authentication.SymmetricKey.PrimaryKey}";
                return MethodResponseMessage.CreateReponseMessage(req, HttpStatusCode.OK, deviceConnectionString);
            }
        }
        catch (Exception ex) { Debug.WriteLine(ex.Message); }

        return MethodResponseMessage.CreateReponseMessage(req, HttpStatusCode.BadRequest, "Bad Request");
    }
}
