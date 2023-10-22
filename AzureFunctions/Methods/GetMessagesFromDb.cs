using System.Diagnostics;
using System.Net;
using AzureFunctions.DataContext;
using AzureFunctions.Models;
using Grpc.Core;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Serialization.HybridRow;
using Microsoft.Azure.Devices;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace AzureFunctions.Methods
{
    public class GetMessagesFromDb
    {
        private readonly CosmosClient _cosmosClient;
        private readonly Container _container;
        private readonly IConfiguration _configuration;

        public GetMessagesFromDb(IConfiguration configuration)
        {
            _configuration = configuration;

            _cosmosClient = new CosmosClient(_configuration.GetConnectionString("CosmosDb"));
            var cosmosdb = _cosmosClient.GetDatabase("gm");
            _container = cosmosdb.GetContainer("Messages");

        }

        [Function("GetMessagesFromDb")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequestData req)
        {
            try
            {
                var deviceId = req.Query["deviceId"];
                if (!string.IsNullOrEmpty(deviceId))
                {
                    var message = _container.GetItemLinqQueryable<DeviceToCloudMessage>(true)
                        .Where(x => x.DeviceId == deviceId)
                        .OrderByDescending(x => x.CreatedOn).ToList().FirstOrDefault();

                    if (message is not null)
                        return MethodResponseMessage.CreateReponseMessage(req, HttpStatusCode.OK, JsonConvert.SerializeObject(message));
                }
            }
            catch (Exception ex) { Debug.WriteLine(ex.Message); }
            
            return MethodResponseMessage.CreateReponseMessage(req, HttpStatusCode.BadRequest, "Bad Request");
        }
    }
}
