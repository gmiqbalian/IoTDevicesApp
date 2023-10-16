using AzureFunctions.Models;
using Microsoft.EntityFrameworkCore;

namespace AzureFunctions.DataContext;

public class CosmosDbContext : DbContext
{
    public CosmosDbContext()
    {

    }
    public CosmosDbContext(DbContextOptions<CosmosDbContext> options) : base(options)
    {

    }
    public DbSet<DeviceToCloudMessage> Messages { get; set; }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DeviceToCloudMessage>(entity =>
        {
            entity.HasKey(d => d.MessageId);
            entity.ToContainer("Messages");
            entity.HasPartitionKey(entity => entity.PartitionKey);
        });
    }
}
