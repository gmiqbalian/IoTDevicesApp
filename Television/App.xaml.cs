using DataLibrary.Contexts;
using DataLibrary.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Television
{
    public partial class App : Application
    {
        private static IHost? _tvAppHost;
        public App()
        {
            _tvAppHost = Host.CreateDefaultBuilder()
                .ConfigureAppConfiguration(config =>
                {
                    config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
                })
                .ConfigureServices((config, services) =>
                {
                    services.AddDbContext<DataContext>(x => x.UseSqlite("Data Source=mydatabase.sqlite.db"));

                    services.AddSingleton<Configuration>();
                    services.AddSingleton<MainWindow>();
                    services.AddSingleton<NetworkService>();
                    services.AddSingleton<DeviceManager>();
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
}
