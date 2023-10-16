using DataLibrary.Contexts;
using DataLibrary.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Windows;

namespace Television;

public partial class App : Application
{
    public static IHost? _tvAppHost { get; set; }
    public App()
    {
        _tvAppHost = Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration(config =>
            {
                config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
            })
            .ConfigureServices((config, services) =>
            {
                services.AddDbContext<DataContext>(x => x.UseSqlite("Data Source=Database.sqlite.db"));

                services.AddSingleton<MainWindow>();
                services.AddSingleton<NetworkService>();
                services.AddSingleton(new DeviceManager(
                    config.Configuration.GetConnectionString("apiurl")!,
                    "tv"));
            })
            .Build();
    }
    protected override async void OnStartup(StartupEventArgs e)
    {
        await _tvAppHost!.StartAsync();

        var mainWindow = _tvAppHost.Services.GetRequiredService<MainWindow>();
        mainWindow.Show();

        base.OnStartup(e);
    }
}
