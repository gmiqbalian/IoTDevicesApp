using Azure.Messaging.EventHubs;
using AzureFunctions.DataContext;
using AzureFunctions.Models;
using Microsoft.Azure.Functions.Worker;
using System.Text;

namespace AzureFunctions.Methods;

public class SaveMessagesToCloud
{
    private readonly CosmosDbContext _dbContext;
    public SaveMessagesToCloud(CosmosDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [Function(nameof(SaveMessagesToCloud))]
    public async Task Run([EventHubTrigger("iothub-ehub-kyh-iothub-25232418-35685cca60", Connection = "IotHubEndpoint")] EventData[] events)
    {
        foreach (EventData @event in events)
        {
            var message = new DeviceToCloudMessage
            {
                MessageId = DateTime.Now.ToString(),
                DeviceId = @event.SystemProperties["iothub-connection-device-id"].ToString(),
                Payload = Encoding.UTF8.GetString(@event.Body.ToArray()),
            };
            _dbContext.Messages.Add(message);
            await _dbContext.SaveChangesAsync();
        }
    }
}
