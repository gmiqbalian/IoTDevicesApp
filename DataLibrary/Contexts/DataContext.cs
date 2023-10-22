using DataLibrary.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace DataLibrary.Contexts;

public class DataContext : DbContext
{
    public DataContext()
    {
        Database.EnsureCreated();
        try
        {
            Database.Migrate();
        }
        catch (Exception ex) { Debug.WriteLine(ex.Message); };
    }
    public DataContext(DbContextOptions<DataContext> options) : base(options)
    {
        Database.EnsureCreated();
        try
        {
            Database.Migrate();
        }
        catch (Exception ex) { Debug.WriteLine(ex.Message); };
    }
    public DbSet<DeviceConfig> DeviceConfig { get; set; }

}
