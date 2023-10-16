using System.Net.NetworkInformation;

namespace DataLibrary.Services;
public class NetworkService
{
    public async Task<string> TestConnectivityAsync(string ipAddress = "8.8.8.8")
    {
        bool connectionStatus;

        try
        {
            using var ping = new Ping();
            var response = await ping.SendPingAsync(ipAddress, 1000, new byte[32], new());

            connectionStatus = response.Status == IPStatus.Success;
        }
        catch { connectionStatus = false; }

        return connectionStatus ? "Connected" : "Disconnected";
    }
}
