using System.Text.Json.Nodes;

namespace DataLibrary.Models;

public class LatestCloudMessage
{
    public string MessageId { get; set; }
    public string? DeviceId { get; set; }
    public string? Payload { get; set; }
    public string PartitionKey { get; set; }
    public DateTime CreatedOn { get; set; }
}
