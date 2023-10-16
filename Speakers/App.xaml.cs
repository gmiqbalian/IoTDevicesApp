using DataLibrary.Contexts;
using DataLibrary.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Windows;

namespace Speakers;

public partial class App : Application
{
    private static IHost? _speakerAppHost { get; set; }
    public App()
    {
        _speakerAppHost = Host.CreateDefaultBuilder()
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
                    "speakers"));
            })
            .Build();
    }
    protected override async void OnStartup(StartupEventArgs e)
    {
        await _speakerAppHost!.StartAsync();

        var mainWindow = _speakerAppHost.Services.GetRequiredService<MainWindow>();
        mainWindow.Show();

        base.OnStartup(e);
    }
}
