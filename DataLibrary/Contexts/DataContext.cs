using DataLibrary.Entities;
using Microsoft.EntityFrameworkCore;
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
    public DbSet<DeviceConfig> DeviceConfiguration { get; set; }
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite("Data Source=Database.sqlite.db");
    }
}
