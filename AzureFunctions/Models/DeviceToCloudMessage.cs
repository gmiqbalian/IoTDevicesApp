namespace AzureFunctions.Models;

public class DeviceToCloudMessage
{
    public string MessageId { get; set; }
    public string? DeviceId { get; set; }
    public string? Payload { get; set; }
    public string PartitionKey { get; set; } = "Message";
    public DateTime CreatedOn { get; set; } = DateTime.Now;
}
